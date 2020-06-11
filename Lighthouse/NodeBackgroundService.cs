﻿using Lighthouse.State;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private Cluster Cluster { get; }
        private ILogger<NodeBackgroundService> Logger { get; }

        public NodeBackgroundService(ILogger<NodeBackgroundService> logger, Cluster cluster)
        {
            ElectionTimer = new Timer(OnElectionTimeout);
            Queue = new BlockingCollection<Task>();
            Logger = logger;
            Cluster = cluster;
        }

        private void OnElectionTimeout(object _)
        {
            try
            {
                var Node = Cluster.Node;

                Logger.LogDebug($"Election timeout, current role: {Node.Role}");

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
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during handling election timeout.");
            }

            ElectionTimer.Change(300, -1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Starting background service.");

            // join the cluster
            await Cluster.Initialize();

            Logger.LogInformation("Starting election timer");

            stoppingToken.Register(() => ElectionTimer.Change(-1, -1));

            ElectionTimer.Change(300, -1);

            foreach (var task in Queue.GetConsumingEnumerable(stoppingToken))
            {
                task.Start();
                await task;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            ElectionTimer.Dispose();
        }
    }
}
