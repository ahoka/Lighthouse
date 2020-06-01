using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public class Cluster
    {
        public IEnumerable<ClusterMember> Members => _members;
        public RaftNodePersistence RaftNodePersistence { get; }
        public RaftConfiguration RaftConfiguration { get; }

        private List<ClusterMember> _members;

        public Cluster(IOptions<RaftConfiguration> raftConfiguration, RaftNodePersistence raftNodePersistence)
        {
            RaftNodePersistence = raftNodePersistence;
            RaftConfiguration = raftConfiguration.Value;
            _members = new List<ClusterMember>();
        }

        public async Task Initialize()
        {
            var nodeConfig = await RaftNodePersistence.ReadAsync();
            if (nodeConfig == null)
            {
                // need to join the cluster
            }
            else
            {
                _members = nodeConfig.Peers.Select(p => new ClusterMember(p.NodeId, p.Address)).ToList();
            }
        }
    }
}
