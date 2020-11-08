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

        private Cluster Cluster { get; }
        private ILogger Logger { get; }

        private Random Rand = new Random();

        public NodeBackgroundService(ILogger logger, Cluster cluster)
        {
            Queue = new BlockingCollection<Task>();
            Logger = logger;
            Cluster = cluster;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.Information("Starting background service.");

            // join the cluster
            await Cluster.Initialize();

            Logger.Information("Starting election timer");

            Cluster.ResetElectionTimer();

            foreach (var task in Queue.GetConsumingEnumerable(stoppingToken))
            {
                task.Start();
                await task;
            }
        }

        public void Dispose()
        {
        }
    }
}
