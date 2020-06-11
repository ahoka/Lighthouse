using Grpc.Net.Client;
using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Lighthouse.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        private ILogger<Cluster> Logger { get; }
        public ILoggerFactory LoggerFactory { get; }

        public Cluster(IOptions<RaftConfiguration> raftConfiguration, RaftNodePersistence raftNodePersistence, ILogger<Cluster> logger, ILoggerFactory loggerFactory)
        {
            RaftNodePersistence = raftNodePersistence;
            RaftConfiguration = raftConfiguration.Value;
            _members = new ConcurrentDictionary<Guid, ClusterMember>();
            Logger = logger;
            LoggerFactory = loggerFactory;
        }

        public async Task Initialize()
        {
            try
            {
                var nodeConfig = await RaftNodePersistence.ReadAsync();
                if (nodeConfig == null)
                {
                    Logger.LogInformation("Bootstrapping node.");

                    _node = new Node(Guid.NewGuid());

                    var expectedPeers = RaftConfiguration.Join.ToList();
                    while (_members.Count < 1)
                    {
                        foreach (var peer in expectedPeers.ToList())
                        {
                            try
                            {
                                var channel = GrpcChannel.ForAddress(peer, new GrpcChannelOptions()
                                {
                                    LoggerFactory = LoggerFactory,
                                    
                                });
                                var client = new Membership.MembershipClient(channel);

                                var result = await client.JoinClusterAsync(new Join()
                                {
                                    NodeInfo = new NodeInfo()
                                    {
                                        Address = RaftConfiguration.Address.ToString(),
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
                                Logger.LogError(ex, $"Cannot contact peer '{peer}', skipping...");
                            }
                        }

                        if (_members.Count < 1)
                        {
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
                Logger.LogError(ex, "Error during node initialization.");
            }
        }
    }
}
