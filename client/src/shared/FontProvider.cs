using SkiaSharp;
using Svg.Skia.TypefaceProviders;

namespace OpenGaugeClient
{
    public class FontProvider(FontCache fontCache) : ITypefaceProvider
    {
        readonly FontCache _fontCache = fontCache;

        public string AddFontFileAndGetFamilyName(string absolutePath)
        {
            return _fontCache.AddFontFileAndGetFamilyName(absolutePath);
        }

        public SKTypeface? FromFamilyName(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            return _fontCache.FromFamilyName(familyName, weight, width, slant);
        }
    }
}
