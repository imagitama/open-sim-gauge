using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;
using System.Globalization;

namespace OpenGaugeClient
{
    public class GaugeRenderer
    {
        private Gauge _gauge;
        private GaugeRef _gaugeRef;
        private int _canvasWidth;
        private int _canvasHeight;
        private double _renderScaling;
        private Func<string, string, object?> _getSimVarValue { get; set; }
        private ImageCache _imageCache;
        private FontProvider _fontProvider;
        private SvgCache _svgCache;
        private bool _debug;

        public GaugeRenderer(Gauge gauge, GaugeRef gaugeRef, int canvasWidth, int canvasHeight, double renderScaling, ImageCache imageCache, FontProvider fontProvider, SvgCache svgCache, Func<string, string, object?> getSimVarValue, bool debug = false)
        {
            _gauge = gauge;
            _gaugeRef = gaugeRef;
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
            _renderScaling = renderScaling;
            _imageCache = imageCache;
            _fontProvider = fontProvider;
            _svgCache = svgCache;
            _getSimVarValue = getSimVarValue;
            _debug = debug;
        }

        public void DrawGaugeLayers(DrawingContext ctx, bool useCachedPositions = true, bool? disableClipping = null)
        {
            var layersToDraw = _gauge.Layers.ToArray().Reverse().ToArray();

            var (gaugeOriginX, gaugeOriginY) = _gauge.Origin.Resolve(_gauge.Width, _gauge.Height, useCachedPositions);

            var gaugeConfigPath = !string.IsNullOrEmpty(_gaugeRef.Path) ? _gaugeRef.Path : PathHelper.GetFilePath("config.json");

            var (x, y) = _gaugeRef.Position.Resolve(_canvasWidth, _canvasHeight, useCachedPositions);
            var scale = _gaugeRef.Scale;

            if (_gaugeRef.Width is double targetWidth && _gauge.Width != 0)
            {
                scale = targetWidth / _gauge.Width * _gaugeRef.Scale;
            }

            var gaugeTransform =
                Matrix.CreateScale(scale, scale) *
                Matrix.CreateTranslation(x, y);

            using (ctx.PushTransform(gaugeTransform))
            {
                using (ctx.PushTransform(Matrix.CreateTranslation(-gaugeOriginX, -gaugeOriginY)))
                {
                    if (ConfigManager.Debug || _debug == true)
                    {
                        DrawGaugeDebug(ctx, _gauge, x, y);
                    }

                    Geometry? clipGeometry = new RectangleGeometry(new Rect(0, 0, _gauge.Width, _gauge.Height));

                    if (_gauge.Clip != null && !string.IsNullOrEmpty(_gauge.Clip.Image))
                    {
                        if (_gauge.Clip.Image == null)
                            throw new Exception("Clip must have an image");

                        var clipConfig = _gauge.Clip;

                        var clipImagePath = Path.Combine(Path.GetDirectoryName(gaugeConfigPath)!, clipConfig!.Image!);
                        var absoluteClipImagePath = PathHelper.GetFilePath(clipImagePath);


                        var clipWidth = clipConfig.Width ?? _gauge.Width;
                        var clipHeight = clipConfig.Height ?? _gauge.Height;

                        var skPath = _svgCache.LoadSKPath(
                            absoluteClipImagePath,
                            clipWidth,
                            clipHeight
                        );

                        var svgPathData = skPath.ToSvgPathData();
                        clipGeometry = Geometry.Parse(svgPathData);

                        var transformedClipGeometry = clipGeometry.Clone();

                        var (clipPosX, clipPosY) = clipConfig.Position.Resolve(_gauge.Width, _gauge.Height, useCachedPositions);

                        var (clipOriginX, clipOriginY) = clipConfig.Origin.Resolve(clipWidth, clipHeight, useCachedPositions);

                        transformedClipGeometry.Transform = new MatrixTransform(
                            Matrix.CreateTranslation(-clipOriginX, -clipOriginY) *
                            Matrix.CreateTranslation(clipPosX, clipPosY)
                        );

                        if (clipConfig.Debug)
                            Console.WriteLine($"Clip {clipPosX},{clipPosY} origin={clipOriginX},{clipOriginY} size={clipWidth}x{clipHeight}");

                        clipGeometry = transformedClipGeometry;
                    }

                    IDisposable? clip = disableClipping == true ? null : ctx.PushGeometryClip(clipGeometry);

                    using (clip)
                    {
                        for (var i = 0; i < layersToDraw.Length; i++)
                        {
                            var layer = layersToDraw[i];

                            if (layer.Skip == true)
                                continue;

                            var (layerOriginX, layerOriginY) = layer.Origin!.Resolve(layer.Width ?? _gauge.Width, layer.Height ?? _gauge.Height, useCachedPositions);

                            var (layerPosX, layerPosY) = layer.Position.Resolve(_gauge.Width, _gauge.Height, useCachedPositions);

                            double rotationAngle = 0;
                            double offsetX = 0;
                            double offsetY = 0;

                            string translateXValue = "";
                            string translateYValue = "";
                            string rotationValue = "";
                            string pathValue = "";

                            if (layer.Transform?.Rotate?.Var != null && layer.Transform.Rotate.Skip != true)
                            {
                                var rotateConfig = layer.Transform.Rotate;
                                var simVarName = rotateConfig.Var.Name;
                                var simVarUnit = rotateConfig.Var.Unit;
                                var varValue = rotateConfig.Override != null ? rotateConfig.Override : _getSimVarValue(simVarName, simVarUnit);

                                rotationAngle = ComputeValue(rotateConfig, varValue, layer);

                                rotationValue = varValue == null
                                    ? "null"
                                    : $"{Math.Truncate(Convert.ToDouble(varValue) * 10) / 10:F1}=>{Math.Truncate(rotationAngle * 10) / 10:F1}";

                                if (rotateConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] Rotate '{simVarName}' ({simVarUnit}) {varValue} => {rotationAngle}° pos={layerPosX},{layerPosY} origin={layerOriginX},{layerOriginY}");
                            }

                            if (layer.Transform?.TranslateX?.Var != null && layer.Transform.TranslateX.Skip != true)
                            {
                                var translateConfig = layer.Transform.TranslateX;
                                var simVarName = translateConfig.Var.Name;
                                var simVarUnit = translateConfig.Var.Unit;
                                var varValue = translateConfig.Override != null ? translateConfig.Override : _getSimVarValue(simVarName, simVarUnit);

                                offsetX = ComputeValue(translateConfig, varValue);

                                translateXValue = varValue == null
                                    ? "null"
                                    : $"{Math.Truncate(Convert.ToDouble(varValue) * 10) / 10:F1}=>{Math.Truncate(offsetX * 10) / 10:F1}";

                                if (translateConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] TranslateX '{simVarName}' ({simVarUnit}) {varValue} => {offsetX}°");
                            }

                            if (layer.Transform?.TranslateY?.Var != null && layer.Transform.TranslateY.Skip != true)
                            {
                                var translateConfig = layer.Transform.TranslateY;
                                var simVarName = translateConfig.Var.Name;
                                var simVarUnit = translateConfig.Var.Unit;
                                var varValue = translateConfig.Override != null ? translateConfig.Override : _getSimVarValue(simVarName, simVarUnit);

                                offsetY = ComputeValue(translateConfig, varValue);

                                translateYValue = varValue == null
                                    ? "null"
                                    : $"{Math.Truncate(Convert.ToDouble(varValue) * 10) / 10:F1}=>{Math.Truncate(offsetY * 10) / 10:F1}";

                                if (translateConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] TranslateY '{simVarName}' ({simVarUnit}) {varValue} => {offsetY}°");
                            }

                            SKPoint? pathPositionResult = null;

                            if (layer.Transform?.Path?.Var != null && layer.Transform.Path.Skip != true)
                            {
                                var pathConfig = layer.Transform.Path;
                                var simVarName = pathConfig.Var.Name;
                                var simVarUnit = pathConfig.Var.Unit;
                                var varValue = pathConfig.Override != null ? pathConfig.Override : _getSimVarValue(simVarName, simVarUnit);

                                // note for ball position even if requested unit "position" it returned as -1 to 1
                                double value = varValue != null ? (double)varValue : 0;

                                if (pathConfig.Image == null)
                                    throw new Exception("Path transform must have an image");

                                var pathImagePath = Path.Combine(Path.GetDirectoryName(gaugeConfigPath)!, pathConfig!.Image!);
                                var absolutePathImagePath = PathHelper.GetFilePath(pathImagePath);

                                pathPositionResult = GetPathPosition(absolutePathImagePath, pathConfig, value, _gauge, useCachedPositions);

                                pathValue = $"=>{pathPositionResult}";

                                if (pathConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] Path '{simVarName}' ({simVarUnit}) {varValue} => {pathPositionResult}°");
                            }

                            var initialRotation = layer.Rotate;
                            rotationAngle += (double)initialRotation;

                            var initialTranslateX = layer.TranslateX;
                            offsetX += (double)initialTranslateX;

                            var initialTranslateY = layer.TranslateY;
                            offsetY += (double)initialTranslateY;

                            Matrix layerTransform =
                                Matrix.CreateTranslation(-layerOriginX, -layerOriginY) *
                                Matrix.CreateRotation(Math.PI * rotationAngle / 180.0) *
                                Matrix.CreateTranslation(layerPosX + offsetX, layerPosY + offsetY);

                            if (pathPositionResult != null)
                            {
                                layerTransform *= Matrix.CreateTranslation(pathPositionResult.Value.X, pathPositionResult.Value.Y);
                            }

                            if (ConfigManager.Debug == true || layer.Debug == true)
                            {
                                var debugTransform =
                                    Matrix.CreateTranslation(-layerOriginX, -layerOriginY) *
                                    Matrix.CreateTranslation(layerPosX + offsetX, layerPosY + offsetY);

                                using (ctx.PushTransform(debugTransform))
                                {
                                    var strings = new List<string>();

                                    if (!string.IsNullOrEmpty(rotationValue))
                                    {
                                        strings.Add($"{rotationValue}°");
                                    }
                                    if (!string.IsNullOrEmpty(translateXValue))
                                    {
                                        strings.Add($"{translateXValue}px");
                                    }
                                    if (!string.IsNullOrEmpty(translateYValue))
                                    {
                                        strings.Add($"{translateYValue}px");
                                    }

                                    DrawDebugText(ctx, string.Join(",", strings), Brushes.LightBlue, new Point(0, 0), 3);
                                }
                            }

                            using (ctx.PushTransform(layerTransform))
                            {
                                if (layer.Text != null)
                                {
                                    var textRef = layer.Text;
                                    var familyName = textRef.FontFamily;

                                    // TODO: if static text then add to imagecache to save on performance
                                    if (textRef.Font != null)
                                    {
                                        var fontPath = Path.Combine(Path.GetDirectoryName(gaugeConfigPath)!, textRef.Font);
                                        var fontAbsolutePath = PathHelper.GetFilePath(fontPath);
                                        familyName = _fontProvider.AddFontFileAndGetFamilyName(fontAbsolutePath);
                                    }

                                    string text = textRef.Default != null ? textRef.Default : "(no text)";

                                    if (textRef.Var != null)
                                    {
                                        var varName = textRef.Var.Name;
                                        var varType = textRef.Var.Unit;
                                        var varValue = _getSimVarValue(varName, varType);

                                        if (varValue != null && textRef.Template != null)
                                        {
                                            text = string.Format(textRef.Template, varValue);
                                        }
                                    }
                                    else if (textRef.Template != null)
                                    {
                                        text = string.Format(textRef.Template, text);
                                    }

                                    if (text == null)
                                        throw new Exception("Text is null");

                                    int pixelWidth = (int)Math.Ceiling(_gauge.Width * _renderScaling);
                                    int pixelHeight = (int)Math.Ceiling(_gauge.Height * _renderScaling);

                                    Bitmap bmp = SvgUtils.RenderTextSvgToBitmap(
                                        _fontProvider,
                                        text,
                                        0,
                                        0,
                                        pixelWidth,
                                        pixelHeight,
                                        pixelWidth / 2,
                                        pixelHeight / 2,
                                        familyName,
                                        (float)(textRef.FontSize * _renderScaling),
                                        textRef.Color,
                                        pixelWidth,
                                        pixelHeight,
                                        _renderScaling
                                    );

                                    //                                     Bitmap bmp = SvgUtils.RenderTextCrisp(
                                    //                                         text,

                                    //                                         _gauge.Width,

                                    // _gauge.Height,
                                    // "Arial",
                                    // (float)textRef.FontSize,
                                    // textRef.Color
                                    //                                     );

                                    // var srcRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                                    // var destRect = new Rect(0, 0, pixelWidth, pixelHeight);

                                    // var dpw = bmp.PixelSize.Width;
                                    // var dph = bmp.PixelSize.Height;

                                    // Rect destRect = new Rect(0, 0, dpw, dph);

                                    var srcRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                                    var destRect = new Rect(0, 0, layer.Width ?? _gauge.Width, layer.Height ?? _gauge.Height);

                                    ctx.DrawImage(bmp, srcRect, destRect);

                                    if (ConfigManager.Debug || layer.Debug == true)
                                    {
                                        ctx.DrawRectangle(null, new Pen(Brushes.LightBlue, 2), destRect);
                                    }
                                }

                                if (layer.Image != null)
                                {
                                    var imagePath = layer.Image;

                                    if (_gauge.Source != null && !Path.IsPathRooted(imagePath))
                                    {
                                        var baseDir = Path.GetDirectoryName(_gauge.Source);

                                        if (baseDir != null)
                                        {
                                            var newPath = Path.Combine(baseDir, imagePath);
                                            imagePath = newPath;
                                        }
                                    }

                                    Bitmap bmp = _imageCache.Load(imagePath, layer, _renderScaling);

                                    int pixelWidth = (int)Math.Ceiling(_gauge.Width * _renderScaling);
                                    int pixelHeight = (int)Math.Ceiling(_gauge.Height * _renderScaling);

                                    var srcRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                                    var destRect = new Rect(0, 0, layer.Width ?? _gauge.Width, layer.Height ?? _gauge.Height);

                                    using (ctx.PushRenderOptions(new RenderOptions { EdgeMode = EdgeMode.Antialias, BitmapInterpolationMode = BitmapInterpolationMode.HighQuality }))
                                    {
                                        ctx.DrawImage(bmp, srcRect, destRect);
                                    }

                                    if (ConfigManager.Debug || layer.Debug == true)
                                    {
                                        ctx.DrawRectangle(null, new Pen(Brushes.LightBlue, 2), destRect);
                                    }
                                }
                            }

                            if (layer.Debug == true)
                            {
                                var debugX = layerPosX + offsetX;
                                var debugY = layerPosY + offsetY;

                                var transformedOrigin = Matrix.CreateTranslation(debugX, debugY);

                                using (ctx.PushTransform(transformedOrigin))
                                {
                                    const int crossSize = 10;
                                    ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(-crossSize, 0), new Point(crossSize, 0));
                                    ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(0, -crossSize), new Point(0, crossSize));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawGaugeDebug(DrawingContext ctx, Gauge gauge, double x, double y)
        {
            var gaugeRect = new Rect(0, 0, gauge.Width, gauge.Height);

            var pen = new Pen(Brushes.Pink, 1)
            {
                DashStyle = new DashStyle(new double[] { 6, 4 }, 0)
            };
            ctx.DrawRectangle(pen, gaugeRect);

            var canvasWidth = gauge.Width;
            var canvasHeight = gauge.Height;

            var formattedText = new FormattedText(
                $"'{gauge.Name}' {x},{y}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                Brushes.White
            );

            var textX = canvasWidth - formattedText.Width;
            var textY = canvasHeight - formattedText.Height;

            ctx.DrawText(formattedText, new Point(textX - 10, textY - 10));
        }

        private SKPoint GetPathPosition(string svgPath, PathConfig pathConfig, double value, Gauge gauge, bool useCachedPositions)
        {
            var skPath = _svgCache.LoadSKPath(
                svgPath,
                pathConfig.Width * _renderScaling,
                pathConfig.Height * _renderScaling
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

            var (offsetX, offsetY) = pathConfig.Position.Resolve(gauge.Width, gauge.Height, useCachedPositions);

            var x = (float)(relativeX + offsetX);
            var y = (float)(relativeY + offsetY);

            return new SKPoint(x, y);
        }

        static double ComputeValue(TransformConfig config, object? varValue, Layer? layer = null)
        {
            if (varValue == null)
                return 0;

            double value = Convert.ToDouble(varValue);

            if (config.Multiply != null)
                value *= (double)config.Multiply;

            if (config.Invert == true)
                value *= -1;

            var varConfig = config.Var;
            string unit = varConfig.Unit;

            var calibration = config.Calibration;
            if (calibration != null && calibration.Count > 0)
            {
                // clamp
                if (value <= calibration[0].Value)
                    return calibration[0].Degrees;
                if (value >= calibration[^1].Value)
                    return calibration[^1].Degrees;

                // find nearest two calibration points and interpolate
                for (int i = 0; i < calibration.Count - 1; i++)
                {
                    var a = calibration[i];
                    var b = calibration[i + 1];
                    if (value >= a.Value && value <= b.Value)
                    {
                        double t = (value - a.Value) / (b.Value - a.Value);
                        double angle = a.Degrees + t * (b.Degrees - a.Degrees);

                        return angle;
                    }
                }
            }

            // if user doesnt want any clamping or anything
            if (config.Min == null && config.Max == null && config.From == null && config.To == null)
                return value;

            if (unit == "radians")
            {
                // normalize wrap-around radians into a centered -π..+π range
                if (value > Math.PI)
                    value -= 2 * Math.PI;
            }

            // TODO: document this
            double defaultMin, defaultMax;
            switch (unit)
            {
                case "feet": defaultMin = 0; defaultMax = 10000; break;
                case "knots": defaultMin = 0; defaultMax = 200; break;
                case "rpm": defaultMin = 0; defaultMax = 3000; break;
                case "fpm": defaultMin = -2000; defaultMax = 2000; break;
                case "position": defaultMin = -127; defaultMax = 127; break;
                case "radians":
                    defaultMin = -Math.PI;
                    defaultMax = Math.PI;
                    break;
                default:
                    defaultMin = 0;
                    defaultMax = 1;
                    break;
            }

            double inputMin = config.Min ?? defaultMin;
            double inputMax = config.Max ?? defaultMax;
            double outputFrom = config.From ?? 0;
            double outputTo = config.To ?? 1;

            double range = inputMax - inputMin;
            if (range <= 0)
                return outputFrom;

            double normalized;
            if (config is RotateConfig rotate && rotate.Wrap)
            {
                double revolutions = (value - inputMin) / range;
                normalized = revolutions - Math.Floor(revolutions);
            }
            else
            {
                normalized = (value - inputMin) / range;
                normalized = Math.Clamp(normalized, 0, 1);
            }

            var finalValue = outputFrom + (outputTo - outputFrom) * normalized;

            return finalValue;
        }

        private static void DrawDebugText(DrawingContext ctx, string text, IBrush brush, Point pos, double scaleText = 1)
        {
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                12 * scaleText,
                brush
            );

            ctx.DrawText(formattedText, new Point(pos.X + 2, pos.Y + 2));
        }
    }
}