using System.Text.Json;

namespace OpenGaugeClient
{
    public static class PersistanceManager
    {
        public static PersistedState? State { get; private set; }

        private static readonly string FileName = "persist.json";

        public static async Task<PersistedState> LoadState()
        {
            string absoluteFilePath = PathHelper.GetFilePath(FileName, forceToGitRoot: false);

            if (ConfigManager.Config.Debug == true)
                Console.WriteLine($"[PersistanceManager] Loading: {absoluteFilePath}");

            if (!File.Exists(absoluteFilePath))
            {
                State = await CreateStateFile();
                return State;
            }

            try
            {
                string json = await File.ReadAllTextAsync(absoluteFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                State = JsonSerializer.Deserialize<PersistedState>(json, options) ?? new PersistedState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PersistanceManager] Failed to load {absoluteFilePath}: {ex.Message}");
                State = await CreateStateFile();
            }

            return State!;
        }

        public static async Task Persist(string key, object? value)
        {
            string absoluteFilePath = PathHelper.GetFilePath(FileName, forceToGitRoot: false);

            if (State == null)
                State = await LoadState();

            var property = typeof(PersistedState).GetProperty(key);
            if (property != null)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(State, convertedValue);
                }
                catch
                {
                    Console.WriteLine($"[PersistanceManager] Warning: Could not assign {value} to property {key}");
                }
            }
            else
            {
                Console.WriteLine($"[PersistanceManager] Warning: Unknown state key '{key}'");
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string newJson = JsonSerializer.Serialize(State, options);
            await File.WriteAllTextAsync(absoluteFilePath, newJson);

            if (ConfigManager.Config.Debug == true)
                Console.WriteLine($"[PersistanceManager] Updated {key}={value} in {absoluteFilePath}");
        }

        private static async Task<PersistedState> CreateStateFile(PersistedState? defaultState = null)
        {
            string absoluteFilePath = PathHelper.GetFilePath(FileName);

            if (ConfigManager.Config.Debug == true)
                Console.WriteLine($"[PersistanceManager] Creating: {absoluteFilePath}");

            defaultState ??= new PersistedState();

            var options = new JsonSerializerOptions { WriteIndented = true };
            string newJson = JsonSerializer.Serialize(defaultState, options);

            Directory.CreateDirectory(Path.GetDirectoryName(absoluteFilePath)!);
            await File.WriteAllTextAsync(absoluteFilePath, newJson);

            return defaultState;
        }
    }

    public class PersistedState
    {
        public string? LastKnownVehicleName { get; set; }
    }
}
