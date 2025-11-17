using System.Reflection;
using System.Runtime.Loader;
using OpenGaugeAbstractions;

namespace OpenGaugeServer
{
    public static class DataSourceLoader
    {
        public static List<Type> LoadDataSources(string dir)
        {
            var absolutePath = PathHelper.GetFilePath(dir, forceToGitRoot: false);

            if (ConfigManager.Debug)
                Console.WriteLine($"[DataSourceLoader] Load data sources: {absolutePath}");

            var list = new List<Type>();

            if (!Directory.Exists(dir))
                throw new Exception($"Data sources directory missing ({absolutePath})");

            var dllPaths = Directory.GetFiles(absolutePath, "*.dll").Where(dllPath => dllPath.Contains("DataSource")).ToList();

            Console.WriteLine($"Loading {dllPaths.Count} data sources...");

            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                var dllPath = Path.Combine(AppContext.BaseDirectory, "data-sources", name.Name + ".dll");

                if (ConfigManager.Debug)
                    Console.WriteLine($"[DataSourceLoader] Resolve DLL: {dllPath}");

                if (File.Exists(dllPath))
                {
                    return context.LoadFromAssemblyPath(dllPath);
                }
                return null;
            };

            foreach (var dllPath in dllPaths)
            {
                try
                {
                    if (ConfigManager.Debug)
                        Console.WriteLine($"[DataSourceLoader] Load DLL: {dllPath}");

                    var alc = new AssemblyLoadContext(dllPath, isCollectible: true);
                    var asm = alc.LoadFromAssemblyPath(Path.GetFullPath(dllPath)) ?? throw new Exception($"Failed to load DLL {dllPath}");
                    var types = asm.GetTypes()
                        .Where(t => typeof(IDataSource).IsAssignableFrom(t)
                                    && !t.IsAbstract
                                    && !t.IsInterface);

                    if (types.Count() == 0)
                        Console.WriteLine($"[DataSourceLoader] DLL is invalid (no types)");

                    foreach (var type in types)
                    {
                        list.Add(type);

                        if (ConfigManager.Debug)
                            Console.WriteLine($"[DataSourceLoader] Found data source: {dllPath}");
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