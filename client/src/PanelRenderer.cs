using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Layout;
using SkiaSharp;

namespace OpenGaugeClient
{
    public class PanelRenderer : IDisposable
    {
        private Func<string, string, object?> _getSimVarValue { get; set; }
        private Panel _panel;
        private ImageCache _imageCache;
        private FontProvider _fontProvider;
        private SvgCache _svgCache;

        private Window _window;
        public Window Window
        {
            get => _window;
            set => _window = value;
        }

        private Image _imageControl;
        public Image ImageControl
        {
            get => _imageControl;
            set => _imageControl = value;
        }

        public PanelRenderer(Panel panel, ImageCache imageCache, FontProvider fontProvider, SvgCache svgCache, Func<string, string, object?> getSimVarValue)
        {
            _panel = panel;
            _getSimVarValue = getSimVarValue;

            _imageCache = imageCache;
            _fontProvider = fontProvider;
            _svgCache = svgCache;

            var canvas = new Canvas
            {
                ClipToBounds = false
            };

            _imageControl = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            Canvas.SetLeft(_imageControl, 0);
            Canvas.SetTop(_imageControl, 0);
            canvas.Children.Add(_imageControl);

            var startupLocation = panel.Position is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual;

            var background = new SolidColorBrush(
                (panel.Background?.ToColor()) ?? Color.FromRgb(0, 0, 0)
            );

            var debug = ConfigManager.Debug || panel.Debug == true;

            _window = new Window
            {
                Title = panel.Name,
                Content = canvas,
                WindowStartupLocation = startupLocation,
                CanResize = debug,
                SystemDecorations = debug ? SystemDecorations.Full : SystemDecorations.None,
                ExtendClientAreaToDecorationsHint = !debug,
                ExtendClientAreaChromeHints = debug
                        ? Avalonia.Platform.ExtendClientAreaChromeHints.Default
                        : Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome,
                Background = panel.Transparent == true ? Brushes.Transparent : background
            };

            if (panel.Transparent == true)
            {
                _window.TransparencyLevelHint = new[]
                {
                    WindowTransparencyLevel.Transparent
                };
            }

            var screens = _window.Screens.All;

            if (panel.Screen != null)
            {
                if (panel.Screen < 0 || panel.Screen >= screens.Count)
                    throw new Exception($"Screen index {panel.Screen} is invalid (found {screens.Count} screens)");
            }

            var activeScreen = panel.Screen != null ? screens[(int)panel.Screen] : screens[0];
            var bounds = activeScreen.Bounds;

            var width = panel.Width ?? bounds.Width;
            var height = panel.Height ?? bounds.Height;

            _window.Width = width;
            _window.Height = height;

            if (panel.Position != null)
            {
                _window.Position = GetWindowPosition();
            }

            _window.Opened += (_, _) =>
            {
                if (panel.Screen != null)
                {
                    var targetScreen = screens[(int)panel.Screen];
                    var bounds = targetScreen.Bounds;

                    if (panel.Position is not null)
                    {
                        _window.Position = GetWindowPosition();
                    }
                    else
                    {
                        var x = bounds.X + (bounds.Width - _window.Width) / 2.0;
                        var y = bounds.Y + (bounds.Height - _window.Height) / 2.0;
                        _window.Position = new PixelPoint((int)Math.Round(x), (int)Math.Round(y));
                    }
                }
            };

            if (panel.Fullscreen == true)
            {
                _window.WindowState = WindowState.FullScreen;
            }

            if (ConfigManager.Debug || panel.Debug == true)
            {
                _window.PointerPressed += (sender, e) =>
                {
                    if (e.GetCurrentPoint(_window).Properties.IsLeftButtonPressed)
                    {
                        _window.BeginMoveDrag(e);
                    }
                };

                _window.PositionChanged += (_, __) => UpdateWindowTitle();

                _window.PropertyChanged += (sender, e) =>
                {
                    if (e.Property == Window.ClientSizeProperty)
                        UpdateWindowTitle();
                };

            }

            _window.Show();
        }

        void UpdateWindowTitle()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var pos = _window.Position;
                var clientSize = _window.ClientSize;
                var screen = _window.Screens.ScreenFromWindow(_window);

                if (screen is not null)
                {
                    var bounds = screen.Bounds;
                    var workingArea = screen.WorkingArea;

                    var relativeX = pos.X - bounds.X;
                    var relativeY = pos.Y - bounds.Y;

                    _window.Title = $"{_panel.Name} - " +
                        $"({relativeX},{relativeY}) - {clientSize.Width:F0}x{clientSize.Height:F0}";
                }
                else
                {
                    _window.Title = $"{_panel.Name}  " +
                        $"({pos.X},{pos.Y})  Canvas {clientSize.Width:F0}x{clientSize.Height:F0}";
                }
            });
        }

        public PixelPoint GetWindowPosition()
        {
            if (_window == null || _window.Screens.Primary == null || _panel == null)
                throw new Exception("Cannot get window position");

            int screenWidth = _window.Screens.Primary.Bounds.Width;
            int screenHeight = _window.Screens.Primary.Bounds.Height;

            var (posX, posY) = _panel.Position.Resolve(
                screenWidth,
                screenHeight
            );

            int windowWidth = (int)_window.Width;
            int windowHeight = (int)_window.Height;

            var (originX, originY) = _panel.Origin.Resolve(windowWidth, windowHeight);

            var screens = _window.Screens.All;
            var screenIndex = Math.Clamp(_panel.Screen ?? 0, 0, screens.Count - 1);
            var bounds = screens[screenIndex].Bounds;

            int x = bounds.X + (int)posX - (int)originX;
            int y = bounds.Y + (int)posY - (int)originY;

            return new PixelPoint(x, y);
        }

        public void DrawGaugeLayers(DrawingContext ctx, List<Layer> layers, Gauge gauge, GaugeRef gaugeRef)
        {
            var width = (int)_window.Width;
            var height = (int)_window.Height;

            var target = new RenderTargetBitmap(new PixelSize(width, height));

            var (gaugeOriginX, gaugeOriginY) = gauge.Origin.Resolve(gauge.Width, gauge.Height);

            var gaugeConfigPath = gaugeRef.Path != null ? gaugeRef.Path : PathHelper.GetFilePath("config.json");

            foreach (var layer in layers)
            {
                if (layer.Skip == true)
                    continue;

                var (layerOriginX, layerOriginY) = layer.Origin!.Resolve(gauge.Width, gauge.Height);

                var layerPos = layer.Position;
                var (layerPosX, layerPosY) = layerPos.Resolve(gauge.Width, gauge.Height);

                double rotationAngle = 0;
                double offsetX = 0;
                double offsetY = 0;

                string translateXValue = "";
                string translateYValue = "";
                string rotationValue = "";
                string pathValue = "";

                if (layer.Transform?.Rotate != null && layer.Transform.Rotate.Skip != true)
                {
                    var rotateConfig = layer.Transform.Rotate;
                    var simVarName = rotateConfig.Var.Name;
                    var simVarUnit = rotateConfig.Var.Unit;
                    var varValue = rotateConfig.Override != null ? rotateConfig.Override : _getSimVarValue(simVarName, simVarUnit);

                    rotationAngle = ComputeValue(rotateConfig, varValue, layer);

                    rotationValue = varValue == null
                        ? "null"
                        : $"{Math.Truncate(Convert.ToDouble(varValue) * 10) / 10:F1}=>{Math.Truncate(rotationAngle * 10) / 10:F1}";

                    if (layer.Debug == true)
                    {
                        Console.WriteLine($"[PanelRenderer] Rotate '{simVarName}' ({simVarUnit}) {varValue} => {rotationAngle}° pos={layerPosX},{layerPosY} origin={layerOriginX},{layerOriginY}");
                    }
                }

                if (layer.Transform?.TranslateX != null && layer.Transform.TranslateX.Skip != true)
                {
                    var translateConfig = layer.Transform.TranslateX;
                    var simVarName = translateConfig.Var.Name;
                    var simVarUnit = translateConfig.Var.Unit;
                    var varValue = translateConfig.Override != null ? translateConfig.Override : _getSimVarValue(simVarName, simVarUnit);

                    offsetX = ComputeValue(translateConfig, varValue);

                    translateXValue = varValue == null
                        ? "null"
                        : $"{Math.Truncate(Convert.ToDouble(varValue) * 10) / 10:F1}=>{Math.Truncate(offsetX * 10) / 10:F1}";

                    if (layer.Debug == true)
                    {
                        Console.WriteLine($"[PanelRenderer] TranslateX '{simVarName}' ({simVarUnit}) {varValue} => {offsetX}°");
                    }
                }

                if (layer.Transform?.TranslateY != null && layer.Transform.TranslateY.Skip != true)
                {
                    var translateConfig = layer.Transform.TranslateY;
                    var simVarName = translateConfig.Var.Name;
                    var simVarUnit = translateConfig.Var.Unit;
                    var varValue = translateConfig.Override != null ? translateConfig.Override : _getSimVarValue(simVarName, simVarUnit);

                    offsetY = ComputeValue(translateConfig, varValue);

                    translateYValue = varValue == null
                        ? "null"
                        : $"{Math.Truncate(Convert.ToDouble(varValue) * 10) / 10:F1}=>{Math.Truncate(offsetY * 10) / 10:F1}";

                    if (layer.Debug == true)
                    {
                        Console.WriteLine($"[PanelRenderer] TranslateY '{simVarName}' ({simVarUnit}) {varValue} => {offsetY}°");
                    }
                }

                SKPoint? pathPositionResult = null;

                if (layer.Transform?.Path != null && layer.Transform.Path.Skip != true)
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

                    pathPositionResult = GetPathPosition(absolutePathImagePath, pathConfig, value, gauge);

                    pathValue = $"=>{pathPositionResult}";

                    if (layer.Debug == true)
                        Console.WriteLine($"[PanelRenderer] Path '{simVarName}' ({simVarUnit}) {varValue} => {pathPositionResult}°");
                }

                var (x, y) = gaugeRef.Position.Resolve(_window.Width, _window.Height);
                var scale = gaugeRef.Scale;

                if (gaugeRef.Width is double targetWidth && gauge.Width != 0)
                {
                    scale = (targetWidth / gauge.Width) * gaugeRef.Scale;
                }

                var gaugeTransform =
                    Matrix.CreateScale(scale, scale) *
                    Matrix.CreateTranslation(x, y);

                using (ctx.PushTransform(gaugeTransform))
                {
                    if (ConfigManager.Debug || layer.Debug == true)
                    {
                        const int crossSize = 5;
                        ctx.DrawLine(new Pen(Brushes.Orange, 4),
                            new Point(-crossSize, 0), new Point(crossSize, 0));
                        ctx.DrawLine(new Pen(Brushes.Orange, 4),
                            new Point(0, -crossSize), new Point(0, crossSize));
                    }

                    using (ctx.PushTransform(Matrix.CreateTranslation(-gaugeOriginX, -gaugeOriginY)))
                    {
                        if (ConfigManager.Debug || layer.Debug == true)
                        {
                            var gaugeRect = new Rect(0, 0, gauge.Width, gauge.Height);

                            var pen = new Pen(Brushes.Pink, 1)
                            {
                                DashStyle = new DashStyle(new double[] { 6, 4 }, 0)
                            };
                            ctx.DrawRectangle(pen, gaugeRect);
                        }

                        Geometry? clipGeometry = null;

                        if (gauge.Clip != null)
                        {
                            if (gauge.Clip.Image == null)
                                throw new Exception("Clip must have an image");

                            var clipConfig = gauge.Clip;

                            var clipImagePath = Path.Combine(Path.GetDirectoryName(gaugeConfigPath)!, clipConfig!.Image!);
                            var absoluteClipImagePath = PathHelper.GetFilePath(clipImagePath);

                            var clipWidth = clipConfig.Width ?? gauge.Width;
                            var clipHeight = clipConfig.Width ?? gauge.Width;

                            var skPath = _svgCache.LoadSKPath(
                                absoluteClipImagePath,
                                clipWidth,
                                clipHeight
                            );

                            var svgPathData = skPath.ToSvgPathData();
                            clipGeometry = Geometry.Parse(svgPathData);

                            var transformedClipGeometry = clipGeometry.Clone();

                            var (clipPosX, clipPosY) = clipConfig.Position.Resolve(gauge.Width, gauge.Height);

                            var (clipOriginX, clipOriginY) = clipConfig.Origin.Resolve(clipWidth, clipHeight);

                            transformedClipGeometry.Transform = new MatrixTransform(
                                Matrix.CreateTranslation(-clipOriginX, -clipOriginY) *
                                Matrix.CreateTranslation(clipPosX, clipPosY)
                            );

                            if (clipConfig.Debug)
                                Console.WriteLine($"Clip {clipPosX},{clipPosY} origin={clipOriginX},{clipOriginY}");

                            clipGeometry = transformedClipGeometry;
                        }

                        IDisposable? clip = clipGeometry != null ? ctx.PushGeometryClip(clipGeometry) : null;

                        var initialRotation = layer.Rotate;
                        rotationAngle += (double)initialRotation;

                        Matrix layerTransform;

                        var initialTranslateX = layer.TranslateX;
                        offsetX += (double)initialTranslateX;

                        var initialTranslateY = layer.TranslateY;
                        offsetY += (double)initialTranslateY;

                        layerTransform =
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
                                if (!string.IsNullOrEmpty(rotationValue))
                                {
                                    DrawDebugText(ctx, $"{rotationValue}°", Brushes.LightBlue, new Point(0, 0), 3);
                                }
                                if (!string.IsNullOrEmpty(translateXValue))
                                {
                                    DrawDebugText(ctx, $"{translateXValue}px", Brushes.LightBlue, new Point(0, 0), 3);
                                }
                                if (!string.IsNullOrEmpty(translateYValue))
                                {
                                    DrawDebugText(ctx, $"{translateYValue}px", Brushes.LightBlue, new Point(0, 0), 3);
                                }

                                const int crossSize = 10;
                                ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(-crossSize, 0), new Point(crossSize, 0));
                                ctx.DrawLine(new Pen(Brushes.LightBlue, 4), new Point(0, -crossSize), new Point(0, crossSize));
                            }
                        }

                        using (clip)
                        {
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

                                    string text = textRef.Default != null ? (string)textRef.Default : "";

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

                                    Bitmap bmp = SvgUtils.RenderTextSvgToBitmap(
                                        _fontProvider,
                                        text,
                                        0,
                                        0,
                                        gauge.Width,
                                        gauge.Height,
                                        gauge.Width / 2,
                                        gauge.Height / 2,
                                        familyName,
                                        (float)textRef.FontSize,
                                        textRef.Color,
                                        gauge.Width,
                                        gauge.Height
                                    );

                                    var srcRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                                    var destRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);

                                    ctx.DrawImage(bmp, srcRect, destRect);

                                    if (ConfigManager.Debug || layer.Debug == true)
                                    {
                                        ctx.DrawRectangle(null, new Pen(Brushes.LightBlue, 2), destRect);
                                    }
                                }
                                else if (layer.Image != null)
                                {
                                    var imagePath = layer.Image;

                                    if (gauge.Source != null)
                                    {
                                        var baseDir = Path.GetDirectoryName(gauge.Source);

                                        if (baseDir != null && !Path.IsPathRooted(imagePath))
                                        {
                                            imagePath = Path.Combine(baseDir, imagePath);
                                        }
                                    }

                                    Bitmap bmp = _imageCache.Load(imagePath, layer);

                                    var srcRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                                    var destRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);

                                    ctx.DrawImage(bmp, srcRect, destRect);

                                    if (ConfigManager.Debug || layer.Debug == true)
                                    {
                                        ctx.DrawRectangle(null, new Pen(Brushes.LightBlue, 2), destRect);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public SKPoint GetPathPosition(string svgPath, PathConfig pathConfig, double value, Gauge gauge)
        {
            var skPath = _svgCache.LoadSKPath(
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

            var (offsetX, offsetY) = pathConfig.Position.Resolve(gauge.Width, gauge.Height);

            var x = (float)(relativeX + offsetX);
            var y = (float)(relativeY + offsetY);

            return new SKPoint(x, y);
        }

        public static void DrawDebugText(DrawingContext ctx, string text, IBrush brush, Point pos, double scaleText = 1)
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

        public void Dispose()
        {
            _svgCache.Dispose();
            _window?.Close();

            GC.SuppressFinalize(this);
        }
    }
}
