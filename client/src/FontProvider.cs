using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using Svg.Skia;
using Svg.Skia.TypefaceProviders;

namespace OpenGaugeClient
{
    public class FontProvider : ITypefaceProvider, IDisposable
    {
        private readonly Dictionary<string, SKTypeface> _cache = new();
        private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

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
                    var absolutePath = PathHelper.GetFilePath(Path.Combine("Fonts", familyName + ext));

                    if (File.Exists(absolutePath))
                    {
                        using var stream = File.OpenRead(absolutePath);
                        var typeface = SKTypeface.FromStream(stream);
                        
                        var actual = typeface.FamilyName;

                        _cache[actual] = typeface;

                        _aliases[familyName] = actual;
                        _aliases[actual] = actual;

                        Console.WriteLine($"[FontProvider] Loaded font '{key}' family name '{_cache[key].FamilyName}'");

                        return typeface;
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
