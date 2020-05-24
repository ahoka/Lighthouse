using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Lighthouse
{
    public class RaftService : Raft.RaftBase
    {
        private readonly ILogger<RaftService> _logger;
        public RaftService(ILogger<RaftService> logger)
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
