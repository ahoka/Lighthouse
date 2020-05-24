using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Lighthouse
{
    public class RaftService : Protocol.Raft.RaftBase
    {
        private readonly ILogger<RaftService> _logger;
        public RaftService(ILogger<RaftService> logger)
        {
            _logger = logger;
        }

        // 1.  Reply false if term < currentTerm (§5.1)
        // 2.  If votedFor is null or candidateId, and candidate’s log is atleast as up-to-date as receiver’s log, grant vote (§5.2, §5.4)
        public override Task<Protocol.RequestVoteReply> RequestVote(Protocol.RequestVoteRequest request, ServerCallContext context)
        {
            return base.RequestVote(request, context);
        }

        // 1.  Reply false if term < currentTerm (§5.1)
        // 2.  Reply false if log doesn’t contain an entry at prevLogIndexwhose term matches prevLogTerm (§5.3)
        // 3.  If an existing entry conflicts with a new one (same indexbut different terms), delete the existing entry and all thatfollow it (§5.3)
        // 4.  Append any new entries not already in the log
        // 5.  If leaderCommit > commitIndex, set commitIndex =min(leaderCommit, index of last new entry)
        public override Task<Protocol.AppendEntriesReply> AppendEntries(Protocol.AppendEntriesRequest request, ServerCallContext context)
        {
            return base.AppendEntries(request, context);
        }

        // 1.  Reply immediately if term < currentTerm
        // 2.  Create new snapshot file if first chunk (offset is 0)
        // 3.  Write data into snapshot file at given offset
        // 4.  Reply and wait for more data chunks if done is false
        // 5.  Save snapshot file, discard any existing or partial snapshotwith a smaller index
        // 6.  If existing log entry has same index and term as snapshot’slast included entry, retain log entries following it and reply
        // 7.  Discard the entire log
        // 8.  Reset state machine using snapshot contents (and loadsnapshot’s cluster configuration)
        public override Task<Protocol.InstallSnapshotReply> InstallSnapshot(Protocol.InstallSnapshotRequest request, ServerCallContext context)
        {
            return base.InstallSnapshot(request, context);
        }
    }
}
