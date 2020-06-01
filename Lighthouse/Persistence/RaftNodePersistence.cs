using Lighthouse.Configuration;
using Lighthouse.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        public RaftNodePersistence(IOptions<PersistenceConfiguration> config, ILogger<RaftNodePersistence> logger)
        {
            Config = config.Value;
            Logger = logger;
        }

        private ILogger<RaftNodePersistence> Logger { get; }
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
                    Logger.LogInformation("No node configuration present.");
                }
                else
                {
                    Logger.LogError(ex, "Error reading node configuration.");
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
                await File.WriteAllTextAsync(Path.Combine(Config.DataDirectory, "node.json"), data);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error writing node configuration.");
            }
        }
    }
}
