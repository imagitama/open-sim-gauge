using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.Layout;
using SkiaSharp;
using Svg.Skia;
using System.Xml.Linq;

namespace OpenGaugeClient
{
    public class PanelRenderer : IDisposable
    {
        public Func<string, string, object?> _getSimVarValue { get; set; }

        public Panel _panel;
        public Window _window;
        public Image _imageControl;
        private ImageCache _imageCache;
        private SvgCache _svgCache;

        public PanelRenderer(Panel panel, Func<string, string, object?> getSimVarValue)
        {
            Console.WriteLine($"[PanelRenderer] Panel '{panel.Name}' {panel.Width}x{panel.Height} screen={panel.Screen} fullscreen={panel.Fullscreen}");
            
            _imageCache = new ImageCache();
            _svgCache = new SvgCache();

            _panel = panel;

            _getSimVarValue = getSimVarValue;

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
                    Background = background
                };

            var screens =_window.Screens.All;

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
            int x = _panel.Position != null ? _panel.Position[0] : 0;
            int y = _panel.Position != null ? _panel.Position[1] : 0;
            int windowWidth = (int)_window.Width;
            int windowHeight = (int)_window.Height;

            var screens = _window.Screens.All;
            var screenIndex = _panel.Screen ?? 0;
            screenIndex = Math.Clamp(screenIndex, 0, screens.Count - 1);

            var targetScreen = screens[screenIndex];
            var bounds = targetScreen.Bounds;
            
            if (x < 0)
                x = bounds.X + bounds.Width - windowWidth + x;
            else
                x = bounds.X + x;

            if (y < 0)
                y = bounds.Y + bounds.Height - windowHeight + y;
            else
                y = bounds.Y + y;

            return new PixelPoint(x, y);
        }

        public void DrawGaugeLayers(DrawingContext ctx, List<Layer> layers, Gauge gauge, GaugeRef gaugeRef)
        {
            var width = (int)_window.Width;
            var height = (int)_window.Height;

            var target = new RenderTargetBitmap(new PixelSize(width, height));

            double scale = gaugeRef.Scale ?? 1.0;

            var gaugeOriginX = gauge.Origin != null ? gauge.Origin[0] : gauge.Width / 2;
            var gaugeOriginY = gauge.Origin != null ? gauge.Origin[1] : gauge.Height / 2;

            var gaugeConfigPath = gaugeRef.Path != null ? Path.GetDirectoryName(gaugeRef.Path) : PathHelper.GetFilePath("config.json");

            foreach (var layer in layers)
            {
                if (layer.Skip == true)
                    continue;

                var layerOrigin = layer.Origin;
                var layerOriginX = layerOrigin != null ? layerOrigin[0] : gaugeOriginX;
                var layerOriginY = layerOrigin != null ? layerOrigin[1] : gaugeOriginY;

                var layerPos = layer.Position;
                var layerPosX = layerPos != null ? layerPos[0] : layerOriginX;
                var layerPosY = layerPos != null ? layerPos[1] : layerOriginY;

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
                    
                    // double normalizedValue = Math.Clamp((double)varValue / 127.0, -1.0, 1.0);

                    // note for ball position even tho requested unit "position" it returned as -1 to 1
                    // TODO: update docs
                    // TODO: update emulator to return same data
                    double value = varValue != null ? (double)varValue : 0;

                    if (pathConfig.Image == null)
                        throw new Exception("Path transform must have an image");

                    var pathImagePath = Path.Combine(gaugeConfigPath!, pathConfig!.Image!);
                    var absolutePathImagePath = PathHelper.GetFilePath(pathImagePath);
                    
                    pathPositionResult = GetPathPosition(absolutePathImagePath, pathConfig, value);

                    pathValue = $"=>{pathPositionResult}";

                    if (layer.Debug == true)
                    {
                        Console.WriteLine($"[PanelRenderer] Path '{simVarName}' ({simVarUnit}) {varValue} => {pathPositionResult}°");
                    }
                }
                    
                var gaugeTransform =
                    Matrix.CreateScale(scale, scale) * 
                    Matrix.CreateTranslation(gaugeRef.Position[0], gaugeRef.Position[1]);

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
                        var clipRect = new Rect(0, 0, gauge.Width, gauge.Height);

                        if (ConfigManager.Debug || layer.Debug == true)
                        {
                            var pen = new Pen(Brushes.Red, 1)
                            {
                                DashStyle = new DashStyle(new double[] { 6, 4 }, 0)
                            };
                            ctx.DrawRectangle(pen, clipRect);
                        }

                        Geometry? clipGeometry = null;

                        if (gauge.Clip != null)
                        {
                            if (gauge.Clip.Image == null)
                                throw new Exception("Clip must have an image");

                            var clipImagePath = Path.Combine(gaugeConfigPath!, gauge.Clip!.Image!);
                            var absoluteClipImagePath = PathHelper.GetFilePath(clipImagePath);

                            clipGeometry = GetClipGeometry(absoluteClipImagePath, gauge.Clip);

                            var transformedClipGeometry = clipGeometry.Clone();

                            var clipPos = gauge.Clip.Position ?? new int[] { 0, 0 };
                            var clipPosX = clipPos.Length > 0 ? clipPos[0] : 0;
                            var clipPosY = clipPos.Length > 1 ? clipPos[1] : 0;

                            var clipOrigin = gauge.Clip.Origin ?? new int[] { 0, 0 };

                            transformedClipGeometry.Transform = new MatrixTransform(
                                Matrix.CreateTranslation(-clipOrigin[0], -clipOrigin[1]) *
                                Matrix.CreateTranslation(clipPos[0], clipPos[1])
                            );

                            clipGeometry = transformedClipGeometry;
                        }
                                
                        IDisposable? clip = clipGeometry != null ? ctx.PushGeometryClip(clipGeometry) : null;

                        var initialRotation = layer.Rotate != null ? layer.Rotate : 0;

                        Matrix layerTransform;

                        rotationAngle += (double)initialRotation;

                        var initialTranslateX = layer.TranslateX != null ? layer.TranslateX : 0;
                        offsetX += (double)initialTranslateX;

                        var initialTranslateY = layer.TranslateY != null ? layer.TranslateY : 0;
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
                                Matrix.CreateTranslation(layerPosX, layerPosY);   

                            using (ctx.PushTransform(debugTransform))
                            {
                                if (!string.IsNullOrEmpty(rotationValue))
                                {
                                    DrawDebugText(ctx, $"{rotationValue}°", Brushes.LightBlue, new Point(0, 0), 2);
                                }
                                if (!string.IsNullOrEmpty(translateXValue))
                                {
                                    DrawDebugText(ctx, $"{translateXValue}px", Brushes.LightBlue, new Point(-layerOriginX, -layerOriginY), 3);
                                }
                                if (!string.IsNullOrEmpty(translateYValue))
                                {
                                    DrawDebugText(ctx, $"{translateYValue}px", Brushes.LightBlue, new Point(-layerOriginX, -layerOriginY), 3);
                                }

                                const int crossSize = 10;
                                ctx.DrawLine(new Pen(Brushes.Blue, 4), new Point(-crossSize, 0), new Point(crossSize, 0));
                                ctx.DrawLine(new Pen(Brushes.Blue, 4), new Point(0, -crossSize), new Point(0, crossSize));
                            }
                        }
                            
                        using (clip)
                        {
                            using (ctx.PushTransform(layerTransform))
                            {
                                if (layer.Text != null)
                                {
                                    var textLayout = GetText(layer.Text);

                                    if (textLayout == null)
                                        throw new Exception($"Failed to get text for layer '{layer.Name}'");

                                    textLayout.Draw(ctx, new Point(-layerOriginX, -layerOriginY));

                                    if (ConfigManager.Debug || layer.Debug == true)
                                    {
                                        ctx.DrawRectangle(null, new Pen(Brushes.Blue, 2), new Rect(new Point(-layerOriginX, -layerOriginY), new Size(textLayout.Width, textLayout.Height)));
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
                                        ctx.DrawRectangle(null, new Pen(Brushes.Blue, 2), destRect);
                                    }
                                }
                            }   
                        }
                    }
                }
            }
        }

        public SKPoint GetPathPosition(string svgPath, PathConfig pathConfig, double value)
        {
            var skPath = _svgCache.LoadSKPath(
                svgPath,
                pathConfig.Width ?? 600,
                pathConfig.Height ?? 600
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

            float relativeX = position.X - centerX;
            float relativeY = position.Y - centerY;

            float offsetX = 0, offsetY = 0;
            if (pathConfig.Position != null && pathConfig.Position.Length >= 2)
            {
                offsetX = pathConfig.Position[0];
                offsetY = pathConfig.Position[1];
            }

            return new SKPoint(relativeX + offsetX, relativeY + offsetY);
        }


        public Geometry GetClipGeometry(string filePath, ClipConfig clipConfig)
        {
            var skPath = _svgCache.LoadSKPath(
                filePath,
                clipConfig.Width ?? 600,
                clipConfig.Height ?? 600
            );

            var svgPathData = skPath.ToSvgPathData();
            var geometry = Geometry.Parse(svgPathData);

            return geometry;
        }

        void DrawDebugText(DrawingContext ctx, string text, IBrush brush, Point pos, double scaleText = 1)
        {
            var typeface = new Typeface("Arial");

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

        double ComputeValue(TransformConfig config, object? varValue, Layer? layer = null)
        {
            if (varValue == null)
                return 0;

            double value = Convert.ToDouble(varValue);

            var debug = config.Debug == true;

            if (config.Multiply != null)
                value *= (double)config.Multiply;

            if (config.Invert == true)
                value *= -1;
            
            var varConfig = config.Var;
            string unit = varConfig.Unit;

            var calibration = varConfig.Options?.Calibration;
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
            {
                return value;
            }

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
                case "feet":     defaultMin = 0; defaultMax = 10000; break;
                case "knots":    defaultMin = 0; defaultMax = 200; break;
                case "rpm":      defaultMin = 0; defaultMax = 3000; break;
                case "fpm":      defaultMin = -2000; defaultMax = 2000; break;
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
            double outputTo   = config.To ?? 1;
            bool wrap = config.Wrap ?? false;

            double range = inputMax - inputMin;
            if (range <= 0)
                return outputFrom;

            double normalized;
            if (wrap)
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


        TextLayout GetText(TextDef textRef)
        {
            var fontSize = textRef.FontSize ?? 24;    
            
            Color color = Colors.White;

            if (textRef.Color != null)
            {
                color = textRef.Color!.ToColor();
            }

            var fontFamily = !string.IsNullOrEmpty(textRef.FontFamily) ? textRef.FontFamily : "Arial";

            string text = textRef.Default != null ? (string)textRef.Default : "";

            if (textRef.Var != null)
            {
                var varName = textRef.Var.Name;
                var varType = textRef.Var.Unit;
                var varValue = _getSimVarValue(varName, varType);

                if (varValue != null)
                    text = varValue.ToString() ?? "";
            }

            var brush = new SolidColorBrush(color);

            var textLayout = new TextLayout(
                text,
                new Typeface(fontFamily),
                fontSize,
                brush,
                textAlignment: TextAlignment.Left,
                flowDirection: FlowDirection.LeftToRight,
                maxWidth: double.PositiveInfinity
            );

            return textLayout;
        }

        public void Dispose()
        {
            _svgCache.Dispose();
            _window?.Close();
        }
    }
}
