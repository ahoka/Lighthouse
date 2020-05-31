using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public class Cluster
    {
        public IEnumerable<ClusterMember> Members { get; }

        public Cluster(IOptions<RaftConfiguration> options)
        {
            options.Value.Peers.Select(p => new ClusterMember(p.Address));
        }
    }
}
