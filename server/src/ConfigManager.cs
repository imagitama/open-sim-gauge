using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenGaugeServer
{
    class ConfigManager
    {
        public static Config Config { get; set; } = null!;

        public static async Task<Config> LoadConfig()
        {
            string configAbsolutePath = PathHelper.GetFilePath("config.json");

            if (!File.Exists(configAbsolutePath))
            {
                throw new Exception($"Config file not found: {configAbsolutePath}");
            }

            string json = await File.ReadAllTextAsync(configAbsolutePath);

            if (json is null)
            {
                throw new Exception($"Config file is invalid: {configAbsolutePath}");
            }

            try
            {
                var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                var options = new JsonSerializerOptions { 
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var newConfig = JsonSerializer.Deserialize<Config>(json, options);

                if (newConfig == null)
                    throw new InvalidOperationException("Failed to load config.");

                Config = newConfig;

                return newConfig;
            } 
            catch
            {
                Console.WriteLine(json);
                throw;
            }
        }

        public static bool Debug => Config?.Debug == true;
    }

    public class Config
    {
        public bool? Debug { get; set; }
        public required string Source { get; set; } // SimConnect|emulator
        public ServerConfig? Server { get; set; }
        public int? Rate { get; set; } // data source poll rate (which equals network rate)

        // for distribution to clients
        // public Panel[] Gauges { get; set; }
        // public Gauge[] Gauges { get; set; }
    }
    
    public class ServerConfig
    {
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
    }

    public static class SourceName {
        public static string SimConnect = "SimConnect";
        public static string Emulator = "Emulator";
    }
}