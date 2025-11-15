using System.Text;
using System.Text.Json;
using OpenGaugeAbstractions;

namespace OpenGaugeServer
{
    class ConfigManager
    {
        private static Config? _config;
        public static Config Config => _config ?? throw new InvalidOperationException("LoadConfig must be called first");

        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        public static async Task<Config> LoadConfig()
        {
            string configAbsolutePath = PathHelper.GetFilePath("config.json", forceToGitRoot: false);

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

                _config = newConfig;

                return newConfig;
            }
            catch
            {
                Console.WriteLine(json);
                throw;
            }
        }

        public static bool Debug => Config.Debug;
    }
}