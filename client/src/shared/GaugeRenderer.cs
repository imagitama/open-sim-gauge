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
        private int _debugOutputCount = 0;

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

        private sealed class LayerRenderData
        {
            public required Layer Layer { get; set; }
            public double LayerWidth { get; set; }
            public double LayerHeight { get; set; }
            public double LayerOriginX { get; set; }
            public double LayerOriginY { get; set; }
            public double LayerPosX { get; set; }
            public double LayerPosY { get; set; }

            public double OffsetX { get; set; }
            public double OffsetY { get; set; }
            public double RotationAngle { get; set; }

            public SKPoint? PathPosition { get; set; }

            public Matrix FinalTransform { get; set; }

            public string DebugText { get; set; } = "";
        }

        public void DrawGaugeLayers(DrawingContext ctx, bool useCachedPositions = true, bool? disableClipping = null, bool? renderNoVarWarnings = true)
        {
            var layersToDraw = _gauge.Layers.ToArray().Reverse().ToArray();

            var (gaugeOriginX, gaugeOriginY) = _gauge.Origin.Resolve(_gauge.Width, _gauge.Height, useCachedPositions);

            var gaugeConfigPath = !string.IsNullOrEmpty(_gaugeRef.Path) ? _gaugeRef.Path : PathHelper.GetFilePath("client.json");

            var (gaugeX, gaugeY) = _gaugeRef.Position.Resolve(_canvasWidth, _canvasHeight, useCachedPositions);

            var scale = _gaugeRef.Scale;

            if (_gaugeRef.Width is double targetWidth && _gauge.Width != 0)
            {
                scale = targetWidth / _gauge.Width * _gaugeRef.Scale;
            }

            var gaugeTransform =
                Matrix.CreateScale(scale, scale) *
                Matrix.CreateTranslation(gaugeX, gaugeY);

            var layerRenderDatas = new List<LayerRenderData>();

            _debugOutputCount = 0;

            using (ctx.PushTransform(gaugeTransform))
            {
                using (ctx.PushTransform(Matrix.CreateTranslation(-gaugeOriginX, -gaugeOriginY)))
                {
                    if (ConfigManager.Config.Debug || _debug == true || _gauge.Debug == true)
                    {
                        DrawGaugeDebug(ctx, gaugeX, gaugeY, scale);
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

                            var layerWidth = layer.Width != null ? layer.Width.Resolve(_gauge.Width) : _gauge.Width;
                            var layerHeight = layer.Height != null ? layer.Height.Resolve(_gauge.Height) : _gauge.Height;

                            var (layerOriginX, layerOriginY) = layer.Origin.Resolve(layerWidth, layerHeight, useCachedPositions);

                            var (layerPosX, layerPosY) = layer.Position.Resolve(_gauge.Width, _gauge.Height, useCachedPositions);

                            double rotationAngle = 0;
                            double offsetX = 0;
                            double offsetY = 0;

                            if (layer.Transform?.Rotate?.Var != null && layer.Transform.Rotate.Skip != true)
                            {
                                var rotateConfig = layer.Transform.Rotate;
                                var varName = rotateConfig.Var.Name;
                                var unit = rotateConfig.Var.Unit;
                                var varValue = rotateConfig.Override != null ? rotateConfig.Override : _getSimVarValue(varName, unit);

                                if (varValue == null && renderNoVarWarnings == true)
                                    DrawNoVarWarning(ctx, varName, unit);

                                rotationAngle = ComputeValue(rotateConfig, varValue, layer);

                                if (rotateConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] Rotate '{varName}' ({unit}) {varValue} => {rotationAngle}° pos={layerPosX},{layerPosY} origin={layerOriginX},{layerOriginY}");
                            }

                            if (layer.Transform?.TranslateX?.Var != null && layer.Transform.TranslateX.Skip != true)
                            {
                                var translateConfig = layer.Transform.TranslateX;
                                var varName = translateConfig.Var.Name;
                                var unit = translateConfig.Var.Unit;
                                var varValue = translateConfig.Override != null ? translateConfig.Override : _getSimVarValue(varName, unit);

                                if (varValue == null && renderNoVarWarnings == true)
                                    DrawNoVarWarning(ctx, varName, unit);

                                offsetX = ComputeValue(translateConfig, varValue);

                                if (translateConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] TranslateX '{varName}' ({unit}) {varValue} => {offsetX}°");
                            }

                            if (layer.Transform?.TranslateY?.Var != null && layer.Transform.TranslateY.Skip != true)
                            {
                                var translateConfig = layer.Transform.TranslateY;
                                var varName = translateConfig.Var.Name;
                                var unit = translateConfig.Var.Unit;
                                var varValue = translateConfig.Override != null ? translateConfig.Override : _getSimVarValue(varName, unit);

                                if (varValue == null && renderNoVarWarnings == true)
                                    DrawNoVarWarning(ctx, varName, unit);

                                offsetY = ComputeValue(translateConfig, varValue);

                                if (translateConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] TranslateY '{varName}' ({unit}) {varValue} => {offsetY}°");
                            }

                            SKPoint? pathPositionResult = null;

                            if (layer.Transform?.Path?.Var != null && layer.Transform.Path.Skip != true)
                            {
                                var pathConfig = layer.Transform.Path;
                                var varName = pathConfig.Var.Name;
                                var unit = pathConfig.Var.Unit;
                                var varValue = pathConfig.Override != null ? pathConfig.Override : _getSimVarValue(varName, unit);

                                if (varValue == null && renderNoVarWarnings == true)
                                    DrawNoVarWarning(ctx, varName, unit);

                                // note for ball position even if requested unit "position" it returned as -1 to 1
                                double value = varValue != null ? (double)varValue : 0;

                                if (pathConfig.Image == null)
                                    throw new Exception("Path transform must have an image");

                                var pathImagePath = Path.Combine(Path.GetDirectoryName(gaugeConfigPath)!, pathConfig!.Image!);
                                var absolutePathImagePath = PathHelper.GetFilePath(pathImagePath);

                                pathPositionResult = SvgUtils.GetPathPosition(_svgCache, absolutePathImagePath, pathConfig, layerWidth, layerHeight, value, useCachedPositions);

                                if (pathConfig.Debug == true)
                                    Console.WriteLine($"[PanelRenderer] Path '{varName}' ({unit}) {varValue} => {pathPositionResult}");
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

                            using (ctx.PushTransform(layerTransform))
                            {
                                if (layer.Fill != null)
                                {
                                    var rect = new Rect(0, 0, layerWidth, layerHeight);
                                    ctx.FillRectangle(new SolidColorBrush(layer.Fill.ToColor()), rect);
                                }

                                if (layer.Image != null)
                                {
                                    var imagePath = PathHelper.Resolve(_gauge.Source, layer.Image);

                                    Bitmap bmp = _imageCache.Load(imagePath, layer.Width != null ? layer.Width.Resolve(_gauge.Width) : null, layer.Height != null ? layer.Height.Resolve(_gauge.Height) : null, _renderScaling);

                                    int pixelWidth = (int)Math.Ceiling(_gauge.Width * _renderScaling);
                                    int pixelHeight = (int)Math.Ceiling(_gauge.Height * _renderScaling);

                                    var srcRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                                    var destRect = new Rect(0, 0, layerWidth, layerHeight);

                                    using (ctx.PushRenderOptions(new RenderOptions { EdgeMode = EdgeMode.Antialias, BitmapInterpolationMode = BitmapInterpolationMode.HighQuality }))
                                    {
                                        ctx.DrawImage(bmp, srcRect, destRect);
                                    }
                                }

                                if (layer.Text != null)
                                {
                                    var textRef = layer.Text;
                                    var fontPath = textRef.Font;
                                    var familyName = textRef.FontFamily;

                                    var typeface = new Typeface("Arial");

                                    if (fontPath != null)
                                    {
                                        var fullFontPath = Path.Combine(Path.GetDirectoryName(gaugeConfigPath)!, fontPath);
                                        var fontAbsolutePath = PathHelper.GetFilePath(fullFontPath);

                                        typeface = _fontProvider.GetTypefaceFromPath(fontAbsolutePath, familyName);
                                    }
                                    else if (familyName != null)
                                    {
                                        typeface = _fontProvider.GetTypefaceFromFamilyName(familyName);
                                    }

                                    string text = textRef.Default ?? "(no text)";

                                    if (textRef.Var != null)
                                    {
                                        var varName = textRef.Var.Name;
                                        var unit = textRef.Var.Unit;
                                        var varValue = _getSimVarValue(varName, unit);

                                        if (varValue == null && renderNoVarWarnings == true)
                                            DrawNoVarWarning(ctx, varName, unit);

                                        if (varValue != null)
                                        {
                                            if (textRef.Template != null)
                                                text = string.Format(textRef.Template, varValue);
                                            else
                                                text = varValue.ToString()!;
                                        }
                                    }
                                    else if (textRef.Template != null)
                                    {
                                        text = string.Format(textRef.Template, text);
                                    }

                                    if (text == null)
                                        throw new Exception("Text is null");

                                    var formattedText = new FormattedText(
                                        text,
                                        CultureInfo.InvariantCulture,
                                        FlowDirection.LeftToRight,
                                        typeface,
                                        textRef.FontSize,
                                        new SolidColorBrush(textRef.Color != null ? textRef.Color.ToColor() : Colors.White)
                                    );

                                    var textX = TextUtils.GetHorizontalOffset(layerWidth, formattedText.Width, textRef.Horizontal);
                                    var textY = TextUtils.GetVerticalOffset(layerHeight, formattedText.Height, textRef.Vertical);

                                    ctx.DrawText(formattedText, new Point(textX, textY));
                                }
                            }

                            if (layer.Debug == true || (ConfigManager.Config.Debug && layer.Debug != false))
                            {
                                layerRenderDatas.Add(new LayerRenderData
                                {
                                    Layer = layer,
                                    LayerWidth = layerWidth,
                                    LayerHeight = layerHeight,
                                    LayerOriginX = layerOriginX,
                                    LayerOriginY = layerOriginY,
                                    LayerPosX = layerPosX,
                                    LayerPosY = layerPosY,
                                    OffsetX = offsetX,
                                    OffsetY = offsetY,
                                    RotationAngle = rotationAngle,
                                    PathPosition = pathPositionResult,
                                    FinalTransform = layerTransform
                                });
                            }
                        }

                        foreach (var layerRenderData in layerRenderDatas)
                        {
                            using (ctx.PushTransform(layerRenderData.FinalTransform))
                            {
                                const int crossSize = 10;
                                ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(-crossSize, 0), new Point(crossSize, 0));
                                ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(0, -crossSize), new Point(0, crossSize));

                                var strings = new List<string>();

                                if (layerRenderData.RotationAngle != 0)
                                    strings.Add($"rot={Math.Truncate(layerRenderData.RotationAngle)}°");

                                if (layerRenderData.OffsetX != 0)
                                    strings.Add($"x={Math.Truncate(layerRenderData.OffsetX)}px");

                                if (layerRenderData.OffsetY != 0)
                                    strings.Add($"y={Math.Truncate(layerRenderData.OffsetY)}px");

                                if (layerRenderData.PathPosition != null)
                                    strings.Add($"path={Math.Truncate(layerRenderData.PathPosition.Value.X)},{Math.Truncate(layerRenderData.PathPosition.Value.Y)}");

                                DrawDebugText(ctx, string.Join("\n", strings), Brushes.LightBlue, new Point(2, 0), 1);

                                ctx.DrawRectangle(null, new Pen(Brushes.LightBlue, 1), new Rect(0, 0, layerRenderData.LayerWidth, layerRenderData.LayerHeight));
                            }
                        }
                    }
                }
            }
        }

        private void DrawGaugeDebug(DrawingContext ctx, double x, double y, double scale)
        {
            var gaugeRect = new Rect(0, 0, _gauge.Width, _gauge.Height);

            var pen = new Pen(Brushes.Pink, 1)
            {
                DashStyle = new DashStyle([6, 4], 0)
            };
            ctx.DrawRectangle(pen, gaugeRect);

            var canvasWidth = _gauge.Width;
            var canvasHeight = _gauge.Height;

            var formattedText = new FormattedText(
                $"'{_gauge.Name}'\n" +
                $"{_gaugeRef.Position.X},{_gaugeRef.Position.Y} => {x},{y}\n" +
                $"{_gauge.Width}x{_gauge.Height} => {_gauge.Width * scale}x{_gauge.Height * scale}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                Brushes.White
            );

            var textX = canvasWidth - formattedText.Width - 5;
            var textY = canvasHeight + 5;

            ctx.DrawText(formattedText, new Point(textX, textY));

            if (_gauge.Grid != null && _gauge.Grid > 0)
                RenderingHelper.DrawGrid(ctx, _gauge.Width, _gauge.Height, (double)_gauge.Grid);
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

        private void DrawDebugText(DrawingContext ctx, string text, IBrush brush, Point? pos = null, double scaleText = 1)
        {
            var p = pos ?? new Point(0, 0);

            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                12 * scaleText,
                brush
            );

            ctx.DrawText(formattedText, new Point(p.X + 2, p.Y + 2 + (_debugOutputCount * formattedText.Height)));

            _debugOutputCount++;
        }

        private void DrawNoVarWarning(DrawingContext ctx, string varName, string? unit)
        {
            DrawDebugText(ctx, $"Var '{varName}' ({unit}) is empty", Brushes.Orange);
        }
    }
}