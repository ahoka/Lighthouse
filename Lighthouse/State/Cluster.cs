using Grpc.Net.Client;
using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Lighthouse.Protocol;
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

        public Cluster(IOptions<RaftConfiguration> raftConfiguration, RaftNodePersistence raftNodePersistence)
        {
            RaftNodePersistence = raftNodePersistence;
            RaftConfiguration = raftConfiguration.Value;
            _members = new ConcurrentDictionary<Guid, ClusterMember>();
        }

        public async Task Initialize()
        {
            var nodeConfig = await RaftNodePersistence.ReadAsync();
            if (nodeConfig == null)
            {
                foreach (var peer in RaftConfiguration.Join)
                {
                    var channel = GrpcChannel.ForAddress(peer);
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
                        foreach (var m in result.Members)
                        {
                            _members.TryAdd(Guid.Parse(m.NodeId), new ClusterMember(Guid.Parse(m.NodeId), new Uri(m.Address)));
                        }
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
    }
}
