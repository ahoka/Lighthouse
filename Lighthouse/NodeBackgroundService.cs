using Lighthouse.State;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lighthouse
{
    public class NodeBackgroundService : BackgroundService
    {
        private BlockingCollection<Task> Queue { get; }
        private Timer ElectionTimer { get; }
        private Node Node { get; }

        public NodeBackgroundService(Node node)
        {
            Node = node;
            ElectionTimer = new Timer(OnElectionTimeout);
            Queue = new BlockingCollection<Task>();
        }

        private void OnElectionTimeout(object _)
        {
            switch (Node.Role)
            {
                case Role.Follower:
                    // If election timeout elapses without receiving AppendEntries RPC
                    // from current leader or granting vote to candidate: convert to candidate
                    if (Node.PersistentState.VotedFor != null)
                    {
                        Node.PersistentState.CurrentTerm += 1;
                        Node.PersistentState.VotedFor = Node.Id;
                        Node.Role = Role.Candidate;
                        // TODO: Send Request Vote RPC
                    }
                    break;
                case Role.Candidate:
                    // If election timeout elapses: start new election
                    Node.PersistentState.CurrentTerm += 1;
                    Node.PersistentState.VotedFor = Node.Id;
                    // TODO: Send request vote rpc
                    break;
            }

            ElectionTimer.Change(300, -1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ElectionTimer.Change(300, -1);

            foreach (var task in Queue.GetConsumingEnumerable(stoppingToken))
            {
                task.Start();
                await task;
            }
        }
    }
}
