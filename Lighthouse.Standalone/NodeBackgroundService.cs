using Grpc.Core;
using Lighthouse.Protocol;
using Lighthouse.State;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lighthouse
{
    public class NodeBackgroundService : IDisposable
    {
        private BlockingCollection<Task> Queue { get; }
        private Timer ElectionTimer { get; }
        private Timer HeartbeatTimer { get; }

        private Cluster Cluster { get; }
        private ILogger Logger { get; }

        private Random Rand = new Random();

        public NodeBackgroundService(ILogger logger, Cluster cluster)
        {
            ElectionTimer = new Timer(OnElectionTimeout);
            HeartbeatTimer = new Timer(OnHeartBeatTimeout, null, 1000, 1000);
            Queue = new BlockingCollection<Task>();
            Logger = logger;
            Cluster = cluster;
        }

        private IEnumerable<RequestVoteReply> Vote()
        {
            var results = new List<RequestVoteReply>();

            foreach (var peer in Cluster.Members)
            {
                if (peer.NodeId != Cluster.Node.Id)
                {
                    Logger.Debug($"Requesting vote from {peer.Address}");

                    var result = peer.Client.RequestVote(new RequestVoteRequest()
                    {
                        CandidateId = Cluster.Node.Id.ToString(),
                        LastLogIndex = Cluster.Node.PersistentState.Log.LastIndex,
                        LastLogTerm = Cluster.Node.PersistentState.Log.LastIndexTerm,
                        Term = Cluster.Node.PersistentState.CurrentTerm
                    });

                    Logger.Debug($"Vote {(result.VoteGranted ? "Granted" : "Denied")}");

                    results.Add(result);
                }
            }

            return results;
        }

        private void OnHeartBeatTimeout(object _)
        {
            if (Cluster.Node.Role == Role.Leader)
            {
                // TODO only if no appendlogs were issued during the heartbeat period
                foreach (var peer in Cluster.Members)
                {
                    if (peer.NodeId != Cluster.Node.Id)
                    {
                        Logger.Debug($"Sending heartbeat to {peer.Address}");

                        var result = peer.Client.AppendEntries(new AppendEntriesRequest()
                        {
                            LeaderCommit = Cluster.Node.VolatileState.CommitIndex,
                            PrevLogIndex = Cluster.Node.PersistentState.Log.LastIndex,
                            PrevLogTerm = Cluster.Node.PersistentState.Log.LastIndexTerm,
                            LeaderId = Cluster.Node.Id.ToString(),
                            Term = Cluster.Node.PersistentState.CurrentTerm
                        });

                        if (result.Term > Cluster.Node.PersistentState.CurrentTerm)
                        {
                            Logger.Debug($"Learned term {result.Term} > {Cluster.Node.PersistentState.CurrentTerm}, becoming Follower");
                            Cluster.Node.Role = Role.Follower;
                            break;
                        }
                    }
                }
            }
        }

        private void OnElectionTimeout(object _)
        {
            try
            {
                var Node = Cluster.Node;

                Logger.Debug($"Election timeout, current role: {Node.Role}");

                switch (Node.Role)
                {
                    case Role.Follower:
                        // If election timeout elapses without receiving AppendEntries RPC
                        // from current leader or granting vote to candidate: convert to candidate
                        //if (Node.PersistentState.VotedFor != null)
                        //{
                            Logger.Information("Converting to Candidate");
                            Node.Role = Role.Candidate;
                            goto case Role.Candidate;
                        //}
                        //break;
                    case Role.Candidate:
                        // If election timeout elapses: start new election
                        Node.PersistentState.CurrentTerm += 1;
                        Node.PersistentState.VotedFor = Node.Id;
                        // TODO: Send request vote rpc
                        Logger.Debug("Requesting votes from peers");
                        var results = Vote();
                        var term = results.Max(r => r.Term);
                        var voted = results.Count(r => r.VoteGranted);
                        if (term > Node.PersistentState.CurrentTerm)
                        {
                            Logger.Debug($"Learned term {term} > {Node.PersistentState.CurrentTerm}, becoming Follower");
                            Node.Role = Role.Follower;
                            Node.PersistentState.CurrentTerm = term;
                            Node.PersistentState.VotedFor = null;
                        }
                        else
                        {
                            if (voted > Cluster.Members.Count() / 2)
                            {
                                Logger.Debug($"Got {voted} votes out of {Cluster.Members.Count()}, becoming Leader");
                                Node.Role = Role.Leader;
                                Logger.Information("Becoming leader.");
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during handling election timeout.");
            }

            // To prevent split votes in the first place, election timeouts are chosen randomly from a fixed interval (e.g., 150–300ms).
            ElectionTimer.Change(Rand.Next(150, 300), -1);
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.Information("Starting background service.");

            // join the cluster
            await Cluster.Initialize();

            Logger.Information("Starting election timer");

            stoppingToken.Register(() => ElectionTimer.Change(-1, -1));

            ElectionTimer.Change(300, -1);

            foreach (var task in Queue.GetConsumingEnumerable(stoppingToken))
            {
                task.Start();
                await task;
            }
        }

        public void Dispose()
        {
            ElectionTimer.Dispose();
        }
    }
}
