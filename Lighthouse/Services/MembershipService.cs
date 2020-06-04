using Grpc.Core;
using Lighthouse.Protocol;
using Lighthouse.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.Services
{
    public class MembershipService : Membership.MembershipBase
    {
        public MembershipService(Cluster cluster)
        {
            Cluster = cluster;
        }

        private Cluster Cluster { get; }

        public override Task<JoinReply> JoinCluster(Join request, ServerCallContext context)
        {
            try
            {
                Cluster.AddMember(new ClusterMember(Guid.Parse(request.NodeInfo.NodeId), new Uri(request.NodeInfo.Address)));

                var members = Cluster.Members.Select(m => new NodeInfo()
                {
                    NodeId = m.NodeId.ToString(),
                    Address = m.Address.ToString()
                });

                var reply = new JoinReply()
                {
                    Success = true,
                };

                reply.Members.AddRange(members);

                return Task.FromResult(reply);
            }
            catch (Exception)
            {
                return Task.FromResult(new JoinReply()
                {
                    Success = false
                });
            }
        }
    }
}
