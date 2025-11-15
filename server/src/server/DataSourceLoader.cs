using System.Runtime.Loader;
using OpenGaugeAbstractions;

namespace OpenGaugeServer
{
    public static class DataSourceLoader
    {
        public static List<IDataSource> LoadDataSources(string dir)
        {
            var absolutePath = PathHelper.GetFilePath(dir, forceToGitRoot: false);

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[DataSourceLoader] Load data sources: {absolutePath}");

            var list = new List<IDataSource>();

            if (!Directory.Exists(dir))
                throw new Exception($"Data sources directory missing ({absolutePath})");

            Console.WriteLine($"Loading data sources...");

            foreach (var dllPath in Directory.GetFiles(absolutePath, "*.dll"))
            {
                try
                {
                    var alc = new AssemblyLoadContext(dllPath, isCollectible: true);
                    var asm = alc.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

                    var types = asm.GetTypes()
                        .Where(t => typeof(IDataSource).IsAssignableFrom(t)
                                    && !t.IsAbstract
                                    && !t.IsInterface);

                    foreach (var type in types)
                    {
                        var instance = (IDataSource)Activator.CreateInstance(type)!;

                        if (string.IsNullOrEmpty(instance.Name))
                            throw new Exception($"Data source name must be set");

                        list.Add(instance);

                        if (ConfigManager.Config.Debug)
                            Console.WriteLine($"[DataSourceLoader] Found data source: {dllPath} name={instance.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load data source {dllPath}: {ex.Message}");

                    if (ConfigManager.Debug)
                        throw;
                }
            }

            return list;
        }
    }
}