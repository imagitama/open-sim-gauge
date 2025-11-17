using SkiaSharp;

namespace OpenGaugeClient
{
    public class FontCache : IDisposable
    {
        private readonly Dictionary<string, SKTypeface> _cache = [];
        private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;
        private Dictionary<string, string> addedFontFiles = [];

        public string AddFontFileAndGetFamilyName(string absolutePath)
        {
            if (addedFontFiles.TryGetValue(absolutePath, out string? value))
                return value;

            if (!File.Exists(absolutePath))
                throw new Exception($"Font does not exist: {absolutePath}");

            using var stream = File.OpenRead(absolutePath);
            var typeface = SKTypeface.FromStream(stream);

            var familyName = typeface.FamilyName;

            _cache[familyName] = typeface;

            _aliases[familyName] = familyName;
            _aliases[familyName] = familyName;

            Console.WriteLine($"[FontProvider] Added font '{familyName}' from file {absolutePath}");

            addedFontFiles[absolutePath] = familyName;

            return familyName;
        }

        public SKTypeface LoadFromPath(string absolutePath, string? familyName = null)
        {
            if (!File.Exists(absolutePath))
                throw new Exception($"Font does not exist: {absolutePath}");

            using var stream = File.OpenRead(absolutePath);
            var typeface = SKTypeface.FromStream(stream);

            var actualFamilyName = typeface.FamilyName;

            if (familyName == null)
                familyName = actualFamilyName;

            var key = familyName.Trim();

            if (_aliases.TryGetValue(key, out var realName))
                key = realName;

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            _cache[actualFamilyName] = typeface;

            _aliases[familyName] = actualFamilyName;
            _aliases[actualFamilyName] = actualFamilyName;

            if (ConfigManager.Config?.Debug == true)
                Console.WriteLine($"[FontProvider] Loaded font '{key}' family name '{_cache[key].FamilyName}'");

            return typeface;
        }

        public SKTypeface? FromFamilyName(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            var key = familyName.Trim();

            if (_aliases.TryGetValue(key, out var realName))
                key = realName;

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            try
            {
                foreach (var ext in new[] { ".ttf", ".otf" })
                {
                    var absolutePath = PathHelper.GetFilePath(Path.Combine("fonts", familyName + ext), forceToGitRoot: false);

                    if (File.Exists(absolutePath))
                    {
                        return LoadFromPath(absolutePath);
                    }
                }

                // use default
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FontProvider] Error loading font '{familyName}': {ex.Message}");
                return null;
            }
        }

        public IEnumerable<string> GetFamilyNames()
        {
            return _cache.Keys;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var font in _cache.Values)
            {
                font.Dispose();
            }

            _cache.Clear();
            _aliases.Clear();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
