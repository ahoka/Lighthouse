using Lighthouse.Configuration;
using Lighthouse.Protocol;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lighthouse.Persistence
{
    public class RaftNodePersistence
    {
        public RaftNodePersistence(PersistenceConfiguration config, ILogger logger)
        {
            Config = config;
            Logger = logger;
        }

        private ILogger Logger { get; }
        private PersistenceConfiguration Config { get; }

        public async Task<RaftNode> ReadAsync()
        {
            try
            {
                var data = await File.ReadAllTextAsync(Path.Combine(Config.DataDirectory, "node.json"));
                return JsonSerializer.Deserialize<RaftNode>(data);
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException)
                {
                    Logger.Information("No node configuration present.");
                }
                else
                {
                    Logger.Error(ex, "Error reading node configuration.");
                }

                return null;
            }
        }

        public async Task WriteAsync(RaftNode raftNode)
        {
            var data = JsonSerializer.Serialize(raftNode, new JsonSerializerOptions()
            {
                WriteIndented = true
            });

            try
            {
                //Logger.Debug($"Persisting cluster state: {data}");
                await File.WriteAllTextAsync(Path.Combine(Config.DataDirectory, "node.json"), data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error writing node configuration.");
            }
        }
    }
}
