using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Lighthouse
{
    public class GreeterService : Raft.RaftBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<RequestVoteReply> RequestVote(RequestVoteRequest request, ServerCallContext context)
        {
            return base.RequestVote(request, context);
        }

        public override Task<AppendEntriesReply> AppendEntries(AppendEntriesRequest request, ServerCallContext context)
        {
            return base.AppendEntries(request, context);
        }

        public override Task<InstallSnapshotReply> InstallSnapshot(InstallSnapshotRequest request, ServerCallContext context)
        {
            return base.InstallSnapshot(request, context);
        }
    }
}
