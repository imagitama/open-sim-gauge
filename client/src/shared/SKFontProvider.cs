using SkiaSharp;
using Svg.Skia.TypefaceProviders;

namespace OpenGaugeClient
{
    public class SKFontProvider(SKFontCache fontCache) : ITypefaceProvider
    {
        readonly SKFontCache _fontCache = fontCache;

        public string AddFontFileAndGetFamilyName(string absolutePath)
        {
            if (!File.Exists(absolutePath))
                throw new Exception($"Cannot add font file '{absolutePath}' - does not exist");

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[FontProvider] Add font file '{absolutePath}'");

            return _fontCache.AddFontFileAndGetFamilyName(absolutePath);
        }

        public SKTypeface? FromFamilyName(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            return _fontCache.FromFamilyName(familyName, weight, width, slant);
        }
    }
}
