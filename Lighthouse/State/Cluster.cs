using Grpc.Core;
using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Lighthouse.Protocol;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public class Cluster
    {
        private Timer ElectionTimer { get; }
        private Timer HeartbeatTimer { get; }

        public Node Node => GetNode();

        private Node GetNode()
        {
            if (_node == null)
            {
                throw new NodeNotInitializedException();
            }

            return _node;
        }

        public IEnumerable<ClusterMember> Members => GetMembers();

        private IEnumerable<ClusterMember> GetMembers()
        {
            if (_node == null)
            {
                throw new NodeNotInitializedException();
            }

            return _members.Values;
        }

        public async Task AddMember(ClusterMember clusterMember)
        {
            if (clusterMember.NodeId == Node.Id)
            {
                Logger.Debug("Not adding self to the peer list.");
                return;
            }

            // TODO: what if duplicate?
            _members.TryAdd(clusterMember.NodeId, clusterMember);

            var raftNode = new RaftNode()
            {
                NodeId = Node.Id,
                Peers = Members.Select(m => new RaftPeer()
                {
                    NodeId = m.NodeId,
                    Address = m.Address
                })
            };

            await RaftNodePersistence.WriteAsync(raftNode);
        }

        private RaftNodePersistence RaftNodePersistence { get; }
        private RaftConfiguration RaftConfiguration { get; }

        private Node _node = null;
        private ConcurrentDictionary<Guid, ClusterMember> _members;

        private ILogger Logger { get; }

        public Cluster(RaftConfiguration raftConfiguration, RaftNodePersistence raftNodePersistence, ILogger logger)
        {
            RaftNodePersistence = raftNodePersistence;
            RaftConfiguration = raftConfiguration;
            _members = new ConcurrentDictionary<Guid, ClusterMember>();
            Logger = logger;
            ElectionTimer = new Timer(OnElectionTimeout);
            HeartbeatTimer = new Timer(OnHeartBeatTimeout);
        }

        private Random Rand = new Random();

        public void ResetElectionTimer()
        {
            ElectionTimer.Change(Rand.Next(150, 300), -1);
        }

        public void ResetHeartbeatTimer()
        {
            HeartbeatTimer.Change(0, -1);
        }

        public async Task Initialize()
        {
            try
            {
                var nodeConfig = await RaftNodePersistence.ReadAsync();
                if (nodeConfig == null)
                {
                    Logger.Information("Bootstrapping node.");

                    _node = new Node(Guid.NewGuid());

                    using var _ = Node.Lock();

                    var expectedPeers = RaftConfiguration.Join.Select(x => $"{x.Host}:{x.Port}").ToList();
                    var expectedPeersCount = expectedPeers.Count;

                    Logger.Information($"Expected peers: {string.Join(", ", expectedPeers)}");
                    while (_members.Count != expectedPeersCount)
                    {
                        foreach (var peer in expectedPeers.ToList())
                        {
                            try
                            {   
                                var channel = new Channel(peer.ToString(), ChannelCredentials.Insecure);//, new GrpcChannelOptions()
                                //{
                                //    LoggerFactory = LoggerFactory
                                //});
                                var client = new Membership.MembershipClient(channel);

                                var result = await client.JoinClusterAsync(new Join()
                                {
                                    NodeInfo = new NodeInfo()
                                    {
                                        Address = $"{RaftConfiguration.Address.Host}:{RaftConfiguration.Address.Port}",
                                        NodeId = Node.Id.ToString()
                                    }
                                });

                                if (result.Success)
                                {
                                    expectedPeers.Remove(peer);

                                    foreach (var m in result.Members)
                                    {
                                       await AddMember(new ClusterMember(Guid.Parse(m.NodeId), m.Address));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, $"Cannot contact peer '{peer}', skipping...");
                            }
                        }

                        if (_members.Count < 1)
                        {
                            Logger.Warning("Not all members reached, retrying in 500ms...");
                            await Task.Delay(500);
                        }
                    }
                }
                else
                {
                    foreach (var m in nodeConfig.Peers.Select(p => new ClusterMember(p.NodeId, p.Address)))
                    {
                        _members.TryAdd(m.NodeId, m);
                    }

                    _node = new Node(nodeConfig.NodeId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during node initialization.");
            }
        }

        private IEnumerable<RequestVoteReply> Vote()
        {
            var results = new List<RequestVoteReply>();

            foreach (var peer in Members)
            {
                if (peer.NodeId != Node.Id)
                {
                    Logger.Debug($"Requesting vote from {peer.Address}");

                    try
                    {
                        var result = peer.Client.RequestVote(new RequestVoteRequest()
                        {
                            CandidateId = Node.Id.ToString(),
                            LastLogIndex = Node.PersistentState.Log.LastLogIndex,
                            LastLogTerm = Node.PersistentState.Log.LastLogTerm,
                            Term = Node.PersistentState.CurrentTerm
                        }, deadline: DateTime.UtcNow + TimeSpan.FromMilliseconds(50));

                        Logger.Debug($"Vote {(result.VoteGranted ? "granted" : "denied")} by {peer.Address}");

                        results.Add(result);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.DeadlineExceeded)
                    {
                        Logger.Warning($"Can't reach {peer.NodeId}@{peer.Address}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Could not call RequestVote RPC");
                    }
                }
            }

            return results;
        }

        private void OnHeartBeatTimeout(object o)
        {
            try
            {
                using var _ = Node.Lock();
                
                if (Node.Role == Role.Leader)
                {
                    // TODO only if no appendlogs were issued during the heartbeat period
                    foreach (var peer in Members)
                    {
                        if (peer.NodeId != Node.Id)
                        {
                            //Logger.Debug($"Sending heartbeat to {peer.Address}");
                            try
                            {
                                var result = peer.Client.AppendEntries(new AppendEntriesRequest()
                                {
                                    LeaderCommit = Node.VolatileState.CommitIndex,
                                    PrevLogIndex = Node.PersistentState.Log.LastLogIndex,
                                    PrevLogTerm = Node.PersistentState.Log.LastLogTerm,
                                    LeaderId = Node.Id.ToString(),
                                    Term = Node.PersistentState.CurrentTerm
                                }, deadline: DateTime.UtcNow + TimeSpan.FromMilliseconds(50));

                                if (result.Term > Node.PersistentState.CurrentTerm)
                                {
                                    Logger.Debug($"Learned term {result.Term} > {Node.PersistentState.CurrentTerm}, becoming Follower");
                                    Node.Role = Role.Follower;
                                    break;
                                }
                            }
                            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.DeadlineExceeded)
                            {
                                //Logger.Debug($"Can't reach {peer.NodeId}@{peer.Address}");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error during AppendEntries RPC");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Information(ex, "Error during heartbeat");
            }

            HeartbeatTimer.Change(Rand.Next(30, 50), -1);
        }

        private void OnElectionTimeout(object o)
        {
            try
            {
                using var _ = Node.Lock();
                //Logger.Debug($"Election timeout, current role: {Node.Role}");

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

                        Logger.Debug("Requesting votes from peers");
                        var results = Vote();
                        var term = results.Count() > 0 ? results.Max(r => r.Term) : 0;
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
                            if (voted >= Members.Count() / 2)
                            {
                                Logger.Debug($"Got {voted} votes out of {Members.Count()}, becoming Leader");
                                Node.Role = Role.Leader;
                                Logger.Information("Becoming leader.");
                                ResetHeartbeatTimer();
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
    }
}
