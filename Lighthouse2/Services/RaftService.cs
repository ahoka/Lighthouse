using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;
using Lighthouse.Protocol;
using Lighthouse.State;
using Serilog;

namespace Lighthouse
{
    public class RaftService : Raft.RaftBase
    {
        private ILogger Logger { get; }
        private Cluster Cluster { get; }

        public RaftService(ILogger logger, Cluster cluster)
        {
            Logger = logger;
            Cluster = cluster;
        }

        public override Task<Protocol.RequestVoteReply> RequestVote(Protocol.RequestVoteRequest request, ServerCallContext context)
        {
            Logger.Debug($"Request vote received from {request.CandidateId}");

            // Reply false if term < currentTerm(�5.1)
            //
            if (Cluster.Node.PersistentState.CurrentTerm > request.Term)
            {
                return Task.FromResult(new Protocol.RequestVoteReply()
                {
                    Term = Cluster.Node.PersistentState.CurrentTerm,
                    VoteGranted = false
                });
            }
            // If RPC request or response contains term T > currentTerm:set currentTerm = T, convert to follower (�5.1)
            //
            else if (request.Term > Cluster.Node.PersistentState.CurrentTerm)
            {
                Cluster.Node.PersistentState.CurrentTerm = request.Term;
                Cluster.Node.Role = Role.Follower;
            }

            // If votedFor is null or candidateId, and candidate�s log is atleast as up-to-date as receiver�s log, grant vote (�5.2, �5.4)
            //
            if (Cluster.Node.PersistentState.VotedFor == null || Cluster.Node.PersistentState.VotedFor == new Guid(request.CandidateId))
            {
                // Raft determines which of two logs is more up-to-date by comparing the index and term of the last entries in the
                // logs. If the logs have last entries with different terms, then the log with the later term is more up-to-date.
                // If the logs end with the same term, then whichever log is longer is more up-to-date.
                //
                if (Cluster.Node.PersistentState.CurrentTerm < request.LastLogTerm ||
                    (Cluster.Node.PersistentState.CurrentTerm == request.LastLogTerm && Cluster.Node.VolatileState.CommitIndex <= request.LastLogIndex))
                {
                    Cluster.Node.PersistentState.VotedFor = new Guid(request.CandidateId);

                    return Task.FromResult(new Protocol.RequestVoteReply()
                    {
                        Term = request.Term,
                        VoteGranted = true
                    });
                }
            }
            
            return Task.FromResult(new Protocol.RequestVoteReply()
            {
                Term = request.Term,
                VoteGranted = false
            });
        }

        // 1.  Reply false if term < currentTerm (�5.1)
        // 2.  Reply false if log doesn�t contain an entry at prevLogIndex whose term matches prevLogTerm (�5.3)
        // 3.  If an existing entry conflicts with a new one (same index but different terms),
        //     delete the existing entry and all that follow it (�5.3)
        // 4.  Append any new entries not already in the log
        // 5.  If leaderCommit > commitIndex, set commitIndex =min(leaderCommit, index of last new entry)
        public override Task<Protocol.AppendEntriesReply> AppendEntries(Protocol.AppendEntriesRequest request, ServerCallContext context)
        {
            Logger.Debug($"Append entries received from: {request.LeaderId}");

            // Reply false if term < currentTerm (�5.1)
            if (Cluster.Node.PersistentState.CurrentTerm > request.Term)
            {
                return Task.FromResult(new Protocol.AppendEntriesReply
                {
                    Term = Cluster.Node.PersistentState.CurrentTerm,
                    Success = false
                });
            }
            // If RPC request or response contains term T > currentTerm:set currentTerm = T, convert to follower (�5.1)
            //
            else if (request.Term > Cluster.Node.PersistentState.CurrentTerm)
            {
                Cluster.Node.PersistentState.CurrentTerm = request.Term;
                Cluster.Node.Role = Role.Follower;
            }

            // Reply false if log doesn�t contain an entry at prevLogIndex whose term matches prevLogTerm (�5.3)
            //
            var prevLog = Cluster.Node.PersistentState.Log.Get(request.PrevLogIndex);
            if (prevLog != null && prevLog.Term != request.PrevLogTerm)
            {
                return Task.FromResult(new Protocol.AppendEntriesReply
                {
                    Term = Cluster.Node.PersistentState.CurrentTerm,
                    Success = false
                });
            }

            if (request.Entries.Count() > 0)
            {
                foreach (var newEntry in request.Entries)
                {
                    // If an existing entry conflicts with a new one (same index but different terms),
                    // delete the existing entry and all that follow it (�5.3)
                    //
                    var existingEntry = Cluster.Node.PersistentState.Log.Get(newEntry.Index);
                    if (existingEntry != null && existingEntry.Term != newEntry.Term)
                    {
                        Cluster.Node.PersistentState.Log.Purge(newEntry.Index);
                    }
                    else
                    {
                        // Append any new entries not already in the log
                        //
                        Cluster.Node.PersistentState.Log.Append(new State.LogEntry()
                        {
                            Index = newEntry.Index,
                            Term = newEntry.Term
                        });
                    }
                }

                // If leaderCommit > commitIndex, set commitIndex =min(leaderCommit, index of last new entry)
                //
                if (request.LeaderCommit > Cluster.Node.VolatileState.CommitIndex)
                {
                    Cluster.Node.VolatileState.CommitIndex = Math.Min(request.LeaderCommit, request.Entries.Last().Index);

                    // TODO: apply log to state machine here and set lastApplied
                }
            }

            return Task.FromResult(new Protocol.AppendEntriesReply()
            {
                Term = request.Term,
                Success = true
            });
        }

        // 1.  Reply immediately if term < currentTerm
        // 2.  Create new snapshot file if first chunk (offset is 0)
        // 3.  Write data into snapshot file at given offset
        // 4.  Reply and wait for more data chunks if done is false
        // 5.  Save snapshot file, discard any existing or partial snapshotwith a smaller index
        // 6.  If existing log entry has same index and term as snapshot�slast included entry, retain log entries following it and reply
        // 7.  Discard the entire log
        // 8.  Reset state machine using snapshot contents (and loadsnapshot�s cluster configuration)
        public override Task<Protocol.InstallSnapshotReply> InstallSnapshot(Protocol.InstallSnapshotRequest request, ServerCallContext context)
        {
            return base.InstallSnapshot(request, context);
        }
    }
}