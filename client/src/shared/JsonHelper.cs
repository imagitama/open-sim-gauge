using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static async Task SaveJson(object content, string relativePath, bool forceToGitRoot = true)
        {
            string newJson = JsonSerializer.Serialize(content, options);

            string absoluteFilePath = PathHelper.GetFilePath(relativePath, forceToGitRoot);

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