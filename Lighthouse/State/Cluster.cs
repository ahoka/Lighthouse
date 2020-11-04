using Grpc.Core;
using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Lighthouse.Protocol;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public class Cluster
    {
        public Node Node => GetNode();

        private Node GetNode()
        {
            if (_node == null)
            {
                throw new NodeNotInitializedException();
            }

            return _node;
        }

        public IEnumerable<ClusterMember> Members => GetMembers();

        private IEnumerable<ClusterMember> GetMembers()
        {
            if (_node == null)
            {
                throw new NodeNotInitializedException();
            }

            return _members.Values;
        }

        public async Task AddMember(ClusterMember clusterMember)
        {
            // TODO: what if duplicate?
            _members.TryAdd(clusterMember.NodeId, clusterMember);

            var raftNode = new RaftNode()
            {
                NodeId = Node.Id,
                Peers = Members.Select(m => new RaftPeer()
                {
                    NodeId = m.NodeId,
                    Address = m.Address
                })
            };

            await RaftNodePersistence.WriteAsync(raftNode);
        }

        private RaftNodePersistence RaftNodePersistence { get; }
        private RaftConfiguration RaftConfiguration { get; }

        private Node _node = null;
        private ConcurrentDictionary<Guid, ClusterMember> _members;

        private ILogger Logger { get; }

        public Cluster(RaftConfiguration raftConfiguration, RaftNodePersistence raftNodePersistence, ILogger logger)
        {
            RaftNodePersistence = raftNodePersistence;
            RaftConfiguration = raftConfiguration;
            _members = new ConcurrentDictionary<Guid, ClusterMember>();
            Logger = logger;
        }

        public async Task Initialize()
        {
            try
            {
                var nodeConfig = await RaftNodePersistence.ReadAsync();
                if (nodeConfig == null)
                {
                    Logger.Information("Bootstrapping node.");

                    _node = new Node(Guid.NewGuid());

                    var expectedPeers = RaftConfiguration.Join.Select(x => $"{x.Host}:{x.Port}").ToList();

                    Logger.Information($"Expected peers: {string.Join(", ", expectedPeers)}");
                    while (_members.Count < 1)
                    {
                        foreach (var peer in expectedPeers.ToList())
                        {
                            try
                            {   
                                var channel = new Channel(peer.ToString(), ChannelCredentials.Insecure);//, new GrpcChannelOptions()
                                //{
                                //    LoggerFactory = LoggerFactory
                                //});
                                var client = new Membership.MembershipClient(channel);

                                var result = await client.JoinClusterAsync(new Join()
                                {
                                    NodeInfo = new NodeInfo()
                                    {
                                        Address = $"{RaftConfiguration.Address.Host}:{RaftConfiguration.Address.Port}",
                                        NodeId = Node.Id.ToString()
                                    }
                                });

                                if (result.Success)
                                {
                                    expectedPeers.Remove(peer);

                                    foreach (var m in result.Members)
                                    {
                                        _members.TryAdd(Guid.Parse(m.NodeId), new ClusterMember(Guid.Parse(m.NodeId), new Uri(m.Address)));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, $"Cannot contact peer '{peer}', skipping...");
                            }
                        }

                        if (_members.Count < 1)
                        {
                            Logger.Warning("No members reached, retrying in 500ms...");
                            await Task.Delay(500);
                        }
                    }
                }
                else
                {
                    foreach (var m in nodeConfig.Peers.Select(p => new ClusterMember(p.NodeId, p.Address)))
                    {
                        _members.TryAdd(m.NodeId, m);
                    }

                    _node = new Node(nodeConfig.NodeId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during node initialization.");
            }
        }
    }
}
