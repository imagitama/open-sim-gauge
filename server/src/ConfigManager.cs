using System.Text;
using System.Text.Json;

namespace OpenGaugeServer
{
    class ConfigManager
    {
        public static Config Config { get; set; } = null!;

        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

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

                var newConfig = JsonSerializer.Deserialize<Config>(json, _options);

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

    [GenerateMarkdownTable]
    /// <summary>
    /// Configuration of the server application that runs on the host machine.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Which data source to use.
        /// <type>'SimConnect' | 'emulator'</type>
        /// </summary>
        public required string Source { get; set; }
        /// <summary>
        /// Override the default IP address and port of the server.
        /// <default>ServerConfig</default>
        /// </summary>
        public ServerConfig Server { get; set; } = new();
        /// <summary>
        /// Override the default poll rate the data source should use (which is also network send rate).
        /// 16.7ms = 60Hz.
        /// </summary>
        public double Rate { get; set; } = 16.7;
        /// <summary>
        /// Log extra output to help diagnose issues.
        /// </summary>
        public bool Debug { get; set; } = false;
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// Override the default IP address and port of the server.
    /// </summary>
    public class ServerConfig
    {
        public string IpAddress { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 1234;
    }

    public static class SourceName
    {
        public static string SimConnect = "SimConnect";
        public static string Emulator = "Emulator";
        public static string Cpu = "Cpu";
    }
}