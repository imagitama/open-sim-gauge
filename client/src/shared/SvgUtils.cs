using System.Text;
using System.Globalization;
using System.Xml.Linq;
using Svg.Skia;
using SkiaSharp;
using Avalonia.Media.Imaging;

namespace OpenGaugeClient
{
    public static class SvgUtils
    {
        public static (float Width, float Height) GetSvgDimensionsFromFileViewBox(string svgPath)
        {
            string absolutePath = PathHelper.GetFilePath(svgPath);

            var doc = XDocument.Load(absolutePath);

            var svgElement = doc.Root
                ?? throw new Exception($"SVG '{svgPath}' has no root <svg> element");

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

        public static (float Width, float Height) GetSvgDimensionsFromViewBox(string svgText)
        {
            var doc = XDocument.Parse(svgText);

            var svgElement = doc.Root
                ?? throw new Exception($"SVG has no root <svg> element");

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
                ?? throw new Exception($"SVG '{svgPath}' does not contain a <path> element");

            var d = pathElement.Attribute("d")?.Value
                ?? throw new Exception($"SVG '{svgPath}' path has no 'd' attribute");

            var svgElement = doc.Root
                ?? throw new Exception($"SVG '{svgPath}' has no root <svg> element");

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
                throw new Exception($"SVG '{svgPath}' has no valid viewBox or explicit width/height");

            float scaleX = (targetWidth != 0 && targetWidth != viewBoxWidth)
                ? targetWidth / viewBoxWidth
                : 1;
            float scaleY = (targetHeight != 0 && targetHeight != viewBoxHeight)
                ? targetHeight / viewBoxHeight
                : 1;

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

        public static SKPoint GetPathPosition(SvgCache svgCache, string svgPath, PathConfig pathConfig, double containerWidth, double containerHeight, double value, bool useCachedPositions)
        {
            var skPath = svgCache.LoadSKPath(
                svgPath,
                pathConfig.Width,
                pathConfig.Height
            );

            using var pathMeasure = new SKPathMeasure(skPath, false);
            float totalLength = pathMeasure.Length;

            value = Math.Clamp(value, -1.0, 1.0);
            double t = (value + 1.0) / 2.0;
            float distance = (float)(t * totalLength);

            if (!pathMeasure.GetPositionAndTangent(distance, out var position, out _))
                return SKPoint.Empty;

            var bounds = skPath.Bounds;
            float centerX = bounds.MidX;
            float centerY = bounds.MidY;

            double relativeX = position.X - centerX;
            double relativeY = position.Y - centerY;

            var (offsetX, offsetY) =
                pathConfig.Position.Resolve(containerWidth, containerHeight, useCachedPositions);

            float containerCenterX = (float)containerWidth / 2f;
            float containerCenterY = (float)containerHeight / 2f;

            float x = (float)(position.X + offsetX - containerCenterX);
            float y = (float)(position.Y + offsetY - containerCenterY);

            return new SKPoint(x, y);
        }
    }
}