using Lighthouse.Configuration;
using Lighthouse.Persistence;
using Lighthouse.State;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lighthouse.Standalone
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            var persistence = new RaftNodePersistence(new PersistenceConfiguration()
            {
                DataDirectory = "data"
            }, logger);
            var cluster = new Cluster(new RaftConfiguration()
            {
                Address = new Uri("localhost:9888"),
                Join = new Uri[] { }
            }, persistence, logger);
            var service = new NodeBackgroundService(logger, cluster);

            await service.ExecuteAsync(CancellationToken.None);
        }
    }
}
