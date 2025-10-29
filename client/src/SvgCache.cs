using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using SkiaSharp;
using Avalonia.Media;

namespace OpenGaugeClient
{
    public class SvgCache : IDisposable
    {
        private readonly Dictionary<string, string> _stringCache = new();
        private readonly Dictionary<string, SKPath> _skPathCache = new();
        private bool _disposed;

        public string LoadStringPath(string svgPath, int? configWidth = null, int? configHeight = null)
        {
            if (_stringCache.TryGetValue(svgPath, out var cached))
                return cached;

            var parsed = SvgUtils.ParseSvgPathData(svgPath, configWidth, configHeight);
            
            _stringCache[svgPath] = parsed.D;

            return parsed.D;
        }

        public SKPath LoadSKPath(string svgPath, int? configWidth = null, int? configHeight = null)
        {
            if (_skPathCache.TryGetValue(svgPath, out var cached))
                return cached;

            var parsed = SvgUtils.ParseSvgPathData(svgPath, configWidth, configHeight);

            var skPath = SKPath.ParseSvgPathData(parsed.D);

            if (parsed.ScaleX != 1 || parsed.ScaleY != 1)
            {
                var matrix = SKMatrix.CreateScale(parsed.ScaleX, parsed.ScaleY);
                skPath.Transform(matrix);
            }

            _skPathCache[svgPath] = skPath;

            return skPath;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var path in _skPathCache.Values)
            {
                path.Dispose();
            }

            _skPathCache.Clear();
            _stringCache.Clear();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
