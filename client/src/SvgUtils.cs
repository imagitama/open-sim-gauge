using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using SkiaSharp;
using Avalonia.Media;

namespace OpenGaugeClient
{
    public static class SvgUtils
    {
        public static (float Width, float Height) GetSvgDimensionsFromViewBox(string svgPath)
        {
            string absolutePath = PathHelper.GetFilePath(svgPath);

            var doc = XDocument.Load(absolutePath);

            // var pathElement = doc.Descendants()
            //     .FirstOrDefault(e => e.Name.LocalName == "path")
            //     ?? throw new Exception($"SVG '{svgPath}' does not contain a <path> element.");

            // var d = pathElement.Attribute("d")?.Value
            //     ?? throw new Exception($"SVG '{svgPath}' path has no 'd' attribute.");

            var svgElement = doc.Root
                ?? throw new Exception($"SVG '{svgPath}' has no root <svg> element.");

            float viewBoxWidth = 0;
            float viewBoxHeight = 0;
            var viewBox = svgElement.Attribute("viewBox")?.Value?.Split(' ');
            if (viewBox?.Length == 4)
            {
                viewBoxWidth = float.Parse(viewBox[2], CultureInfo.InvariantCulture);
                viewBoxHeight = float.Parse(viewBox[3], CultureInfo.InvariantCulture);
            }

            return (viewBoxWidth, viewBoxHeight);
        }

        public static (string D, float ScaleX, float ScaleY) ParseSvgPathData(string svgPath, int? configWidth, int? configHeight)
        {
            string absolutePath = PathHelper.GetFilePath(svgPath);

            var doc = XDocument.Load(absolutePath);

            var pathElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "path")
                ?? throw new Exception($"SVG '{svgPath}' does not contain a <path> element.");

            var d = pathElement.Attribute("d")?.Value
                ?? throw new Exception($"SVG '{svgPath}' path has no 'd' attribute.");

            var svgElement = doc.Root
                ?? throw new Exception($"SVG '{svgPath}' has no root <svg> element.");

            float viewBoxWidth = 0;
            float viewBoxHeight = 0;
            var viewBox = svgElement.Attribute("viewBox")?.Value?.Split(' ');
            if (viewBox?.Length == 4)
            {
                viewBoxWidth = float.Parse(viewBox[2], CultureInfo.InvariantCulture);
                viewBoxHeight = float.Parse(viewBox[3], CultureInfo.InvariantCulture);
            }

            float targetWidth = configWidth ?? ParseSvgLength(svgElement.Attribute("width")?.Value) ?? viewBoxWidth;
            float targetHeight = configHeight ?? ParseSvgLength(svgElement.Attribute("height")?.Value) ?? viewBoxHeight;

            if (viewBoxWidth <= 0 || viewBoxHeight <= 0)
                throw new Exception($"SVG '{svgPath}' has no valid viewBox or explicit width/height.");

            float scaleX = (targetWidth != 0 && targetWidth != viewBoxWidth)
                ? targetWidth / viewBoxWidth
                : 1;
            float scaleY = (targetHeight != 0 && targetHeight != viewBoxHeight)
                ? targetHeight / viewBoxHeight
                : 1;

            Console.WriteLine($"[SvgCache] Loaded SVG '{svgPath}' target={targetWidth}x{targetHeight} viewBox={viewBoxWidth}x{viewBoxHeight} scale={scaleX}x{scaleY}");

            return (d, scaleX, scaleY);
        }

        private static float? ParseSvgLength(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim().Replace("px", "", StringComparison.OrdinalIgnoreCase);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }
    }
}