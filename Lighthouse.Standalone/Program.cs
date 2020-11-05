using Grpc.Core;
using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Lighthouse.Protocol;
using Lighthouse.Services;
using Lighthouse.State;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lighthouse.Standalone
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            var configuration = new RaftConfiguration()
            {
                Address = new Uri(Environment.GetEnvironmentVariable("LH_ADDRESS")),
                Join = Environment.GetEnvironmentVariable("LH_PEERS")?
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => new Uri(x))
            };

            var persistence = new RaftNodePersistence(new PersistenceConfiguration()
            {
                DataDirectory = "/app"
            }, logger);
            var cluster = new Cluster(configuration, persistence, logger);
            var service = new NodeBackgroundService(logger, cluster);

            var server = new Server()
            {
                Services =
                {
                    Raft.BindService(new RaftService(logger, cluster)),
                    Membership.BindService(new MembershipService(cluster, configuration, logger))
                },
                Ports = { new ServerPort("0.0.0.0", 9000, ServerCredentials.Insecure) }
            };

            server.Start();

            await service.ExecuteAsync(CancellationToken.None);
            await server.ShutdownAsync();
        }
    }
}
