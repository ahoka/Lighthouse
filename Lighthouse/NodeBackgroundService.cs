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
            // If election timeout elapses: start new election

            // restart the timer
            ElectionTimer.Change(300, -1);

            Node.Role = Role.Candidate;
            Node.PersistentState.VotedFor = Node.Id;
            // TODO: Send Request Vote RPC
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
