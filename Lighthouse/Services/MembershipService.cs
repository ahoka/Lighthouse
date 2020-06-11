using Grpc.Core;
using Lighthouse.Configuration;
using Lighthouse.Protocol;
using Lighthouse.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.Services
{
    public class MembershipService : Membership.MembershipBase
    {
        private RaftConfiguration Configuration { get; }
        private Cluster Cluster { get; }
        private ILogger<MembershipService> Logger { get; }

        public MembershipService(Cluster cluster, IOptions<RaftConfiguration> raftConfiguration, ILogger<MembershipService> logger)
        {
            Cluster = cluster;
            Configuration = raftConfiguration.Value;
            Logger = logger;
        }

        public override async Task<JoinReply> JoinCluster(Join request, ServerCallContext context)
        {
            try
            {
                await Cluster.AddMember(new ClusterMember(Guid.Parse(request.NodeInfo.NodeId), new Uri(request.NodeInfo.Address)));

                var members = Cluster.Members.Select(m => new NodeInfo()
                {
                    NodeId = m.NodeId.ToString(),
                    Address = m.Address.ToString()
                });

                var reply = new JoinReply()
                {
                    Success = true,
                };

                reply.Members.Add(new NodeInfo() {
                    NodeId = Cluster.Node.Id.ToString(),
                    Address = Configuration.Address.ToString()
                });

                reply.Members.AddRange(members);

                return reply;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to add new node to cluster.");

                return new JoinReply()
                {
                    Success = false
                };
            }
        }
    }
}
