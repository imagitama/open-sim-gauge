namespace OpenGaugeClient
{
    public class GaugeCache
    {
        private readonly Dictionary<string, Gauge> _cache = [];

        public async Task<Gauge> Load(string path)
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached;

            var gauge = await ConfigManager.LoadTypedJson<Gauge>(path, forceToGitRoot: true);

            gauge.Source = PathHelper.GetFilePath(path);

            _cache[path] = gauge;

            return gauge;
        }
    }
}