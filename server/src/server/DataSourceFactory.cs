using System.Reflection;
using OpenGaugeAbstractions;

namespace OpenGaugeServer
{
    public static class DataSourceFactory
    {
        private static Dictionary<string, Type> _pluginTypes = new();

        public static void LoadDataSources(string dir)
        {
            var dataSources = DataSourceLoader.LoadDataSources(dir);

            foreach (var type in dataSources)
            {
                var attr = type.GetCustomAttribute<DataSourceNameAttribute>();
                var name = attr?.Name ?? type.Name;
                var key = name.ToLower();

                _pluginTypes[key] = type;
            }
        }

        public static IDataSource Create(string sourceName)
        {
            string key = sourceName.ToLower();

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[DataSourceFactory] Creating data source '{sourceName}'...");

            if (_pluginTypes.TryGetValue(key, out var type))
                return (IDataSource)Activator.CreateInstance(type, [ConfigManager.Config])!;

            throw new NotSupportedException($"Unknown data source: {sourceName} key={key} keys={string.Join(",", _pluginTypes.Keys)}");
        }
    }
}