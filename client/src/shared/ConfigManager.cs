using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    public static class ConfigManager
    {
        private static Config? _config;
        public static Config Config => _config ?? throw new InvalidOperationException("LoadConfig must be called first");

        public static async Task SavePanel(int panelIndex, Panel panelToSave)
        {
            Config.Panels[panelIndex] = panelToSave;

            await SaveConfig();
        }

        public static async Task AddPanel(Panel panelToAdd)
        {
            Config.Panels.Add(panelToAdd);

            await SaveConfig();
        }

        public static async Task DeletePanel(int panelIndex)
        {
            Config.Panels.RemoveAt(panelIndex);

            await SaveConfig();
        }

        public static async Task SaveGauge(int gaugeIndex, Gauge gaugeToSave)
        {
            Console.WriteLine($"[ConfigManager] Save gauge index={gaugeIndex} gauge={gaugeToSave}");

            Config!.Gauges[gaugeIndex] = gaugeToSave;

            await SaveConfig();
        }

        public static async Task AddGauge(Gauge gaugeToAdd)
        {
            Config.Gauges.Add(gaugeToAdd);

            await SaveConfig();
        }

        public static async Task DeleteGauge(int panelIndex)
        {
            Config.Gauges.RemoveAt(panelIndex);

            await SaveConfig();
        }

        public static async Task<Config> LoadConfig(string? overridePath = null)
        {
            var configPath = overridePath ?? PathHelper.GetFilePath("client.json", forceToGitRoot: false);

            Console.WriteLine($"Load config: {configPath}");

            var newConfig = await LoadTypedJson<Config>(configPath);

            if (_config?.Debug == true || newConfig.Debug)
                Console.WriteLine($"[ConfigManager] Loaded config from {configPath}");

            var _gaugeCache = new GaugeCache();

            foreach (var panel in newConfig.Panels)
            {
                foreach (var gaugeRef in panel.Gauges)
                {
                    Gauge? gauge;

                    if (gaugeRef.Path != null)
                    {
                        gauge = await _gaugeCache.Load(gaugeRef.Path);
                    }
                    else
                    {
                        gauge = newConfig.Gauges.Find(g => g.Name == gaugeRef.Name);
                    }

                    if (gauge == null)
                        Console.WriteLine($"Panel '{panel.Name}' has invalid gauge '{gaugeRef.Name}' or path '{gaugeRef.Path}'");

                    gaugeRef.Gauge = gauge;
                }
            }

            for (var i = 0; i < newConfig.Gauges.Count; i++)
            {
                var gauge = newConfig.Gauges[i];

                if (gauge.Path != null)
                {
                    var path = gauge.Path;

                    var newGauge = await _gaugeCache.Load(path);

                    gauge.Replace(newGauge);

                    newGauge.Source = path;

                    newConfig.Gauges[i] = newGauge;
                }
            }

            _config = newConfig;

            return newConfig;
        }

        public static async Task SaveConfig()
        {
            await SaveJson(Config, "client.json", false);
        }

        public static async Task SaveJson(object content, string relativePath, bool forceToGitRoot = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            string newJson = JsonSerializer.Serialize(content, options);

            string absoluteFilePath = PathHelper.GetFilePath(relativePath, forceToGitRoot);

            if (Config.Debug)
                Console.WriteLine($"Save config: {absoluteFilePath}");

            Directory.CreateDirectory(Path.GetDirectoryName(absoluteFilePath)!);
            await File.WriteAllTextAsync(absoluteFilePath, newJson);
        }

        private static string ExtractPropertyName(string message)
        {
            int start = message.IndexOf('\'');
            int end = message.IndexOf('\'', start + 1);

            if (start >= 0 && end > start)
                return message.Substring(start + 1, end - start - 1);

            return "unknown";
        }

        private static Type? ResolveTypeAtJsonPath(Type rootType, string fullPath)
        {
            string path = fullPath.StartsWith("$.") ? fullPath[2..] : fullPath;

            int lastDot = path.LastIndexOf('.');
            if (lastDot > 0)
                path = path.Substring(0, lastDot);

            Type currentType = rootType;

            if (string.IsNullOrWhiteSpace(path))
                return currentType;

            var segments = path.Split('.');

            foreach (var segment in segments)
            {
                string propName = segment;
                int bracket = segment.IndexOf('[');
                if (bracket >= 0)
                    propName = segment.Substring(0, bracket);

                var prop = currentType.GetProperty(propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null)
                    return null;

                Type propType = prop.PropertyType;

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType)
                    && propType != typeof(string))
                {
                    if (propType.IsArray)
                        currentType = propType.GetElementType()!;
                    else if (propType.IsGenericType)
                        currentType = propType.GetGenericArguments()[0];
                    else
                        return null;
                }
                else
                {
                    currentType = propType;
                }
            }

            return currentType;
        }

        private static bool IsUnknownPropertyError(string msg)
        {
            return msg.Contains("could not be mapped to any .NET member");
        }

        public static async Task<T> LoadTypedJson<T>(string filePath, bool forceToGitRoot = false)
        {
            string absoluteFilePath = PathHelper.GetFilePath(filePath, forceToGitRoot);

            if (!File.Exists(absoluteFilePath))
                throw new Exception($"JSON file not found: {absoluteFilePath}");

            string json = await File.ReadAllTextAsync(absoluteFilePath);

            try
            {
                var reader = new Utf8JsonReader(
                    Encoding.UTF8.GetBytes(json),
                    new JsonReaderOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    }
                );

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };

                var result = JsonSerializer.Deserialize<T>(ref reader, options);
                return result!;
            }
            catch (JsonException ex)
            {
                string unknown = ExtractPropertyName(ex.Message);
                string path = ex.Path ?? "";
                path = path.StartsWith("$.") ? path[2..] : path;

                if (IsUnknownPropertyError(ex.Message))
                {
                    var targetType = ResolveTypeAtJsonPath(typeof(T), path);

                    string available = targetType != null
                        ? string.Join(", ", targetType.GetProperties().Select(p => p.Name))
                        : "unknown";

                    Console.WriteLine(
                        $"Failed to load JSON file {absoluteFilePath}:\n" +
                        $"JSON property '{unknown}' at {path} is not recognized.\n" +
                        $"Available properties: {available}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"JSON property at {path} has unexpected value"
                    );
                }

                throw;
            }
        }
    }
}