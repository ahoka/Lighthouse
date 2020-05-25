using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;
using Lighthouse.State;
using Microsoft.Extensions.Logging;

namespace Lighthouse
{
    public class RaftService : Protocol.Raft.RaftBase
    {
        private readonly ILogger<RaftService> _logger;
        private Node Node { get; }

        public RaftService(ILogger<RaftService> logger, Node node)
        {
            _logger = logger;
            Node = node;
        }

        // 1.  Reply false if term < currentTerm (§5.1)
        // 2.  If votedFor is null or candidateId, and candidate’s log is atleast as up-to-date as receiver’s log, grant vote (§5.2, §5.4)
        public override Task<Protocol.RequestVoteReply> RequestVote(Protocol.RequestVoteRequest request, ServerCallContext context)
        {
            if (Node.PersistentState.CurrentTerm > request.Term)
            {
                return Task.FromResult(new Protocol.RequestVoteReply()
                {
                    Term = Node.PersistentState.CurrentTerm,
                    VoteGranted = false
                }); 
            }

            if (Node.PersistentState.VotedFor == null || Node.PersistentState.VotedFor == new Guid(request.CandidateId))
            {
                // Raft determines which of two logs is more up-to-date by comparing the index and term of the last entries in the
                // logs. If the logs have last entries with different terms, then the log with the later term is more up-to-date.
                // If the logs end with the same term, then whichever log is longer is more up-to-date.
                if (Node.PersistentState.CurrentTerm < request.LastLogTerm ||
                    (Node.PersistentState.CurrentTerm == request.LastLogTerm && Node.VolatileState.CommitIndex <= request.LastLogIndex))
                {
                    Node.PersistentState.VotedFor = new Guid(request.CandidateId);

                    return Task.FromResult(new Protocol.RequestVoteReply()
                    {
                        Term = request.Term,
                        VoteGranted = true
                    });
                }
                else
                {
                    return Task.FromResult(new Protocol.RequestVoteReply()
                    {
                        Term = request.Term,
                        VoteGranted = false
                    });
                }
            }
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
