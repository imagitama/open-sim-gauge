using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using Svg.Skia;
using Svg.Skia.TypefaceProviders;

namespace OpenGaugeClient
{
    public class FontProvider : ITypefaceProvider
    {
        FontCache _fontCache;

        public FontProvider(FontCache fontCache)
        {
            _fontCache = fontCache;
        }

        public string AddFontFileAndGetFamilyName(string absolutePath)
        {
            return _fontCache.AddFontFileAndGetFamilyName(absolutePath);
        }

        public SKTypeface? FromFamilyName(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            // Console.WriteLine($"[FontProvider] Load family '{familyName}'");
            return _fontCache.FromFamilyName(familyName, weight, width, slant);
        }
    }
}
