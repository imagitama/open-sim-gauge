using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Layout;

namespace OpenGaugeClient
{
    public class PanelRenderer : IDisposable
    {
        private readonly Panel _panel;
        private readonly ImageCache _imageCache;
        private readonly GaugeCache _gaugeCache;
        private readonly FontProvider _fontProvider;
        private readonly SvgCache _svgCache;
        private Func<string, string, object?> _getSimVarValue { get; set; }
        private readonly Dictionary<int, GaugeRenderer> _gaugeRenderers = [];

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

        private RenderingHelper _renderer;

        public PanelRenderer(Panel panel, GaugeCache gaugeCache, ImageCache imageCache, FontProvider fontProvider, SvgCache svgCache, Func<string, string, object?> getSimVarValue)
        {
            _panel = panel;
            _gaugeCache = gaugeCache;
            _imageCache = imageCache;
            _fontProvider = fontProvider;
            _svgCache = svgCache;
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

            var debug = ConfigManager.Debug || panel.Debug == true;

            _window = PanelHelper.CreatePanelWindowFromPanel(_panel);

            _window.Content = canvas;

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
                    if (e.Property == TopLevel.ClientSizeProperty)
                        UpdateWindowTitle();
                };
            }

            CreateGaugeRenderers();

            _renderer = new RenderingHelper(_imageControl, RenderFrameAsync, ConfigManager.Config!.Fps, _window);

            _window.Opened += (_, _) => _renderer.Start();
            _window.Closing += (_, _) => _renderer.Dispose();

            _window.Show();
        }

        void CreateGaugeRenderers()
        {
            for (var i = 0; i < _panel.Gauges.Count; i++)
            {
                var gaugeRef = _panel.Gauges[i];

                if (gaugeRef.Gauge == null)
                    throw new Exception("Gauge is null");

                var gaugeRenderer = new GaugeRenderer(
                    gaugeRef.Gauge,
                    (int)_window.Width,
                    (int)_window.Height,
                    _imageCache,
                    _fontProvider,
                    _svgCache,
                    _getSimVarValue
                );

                _gaugeRenderers[i] = gaugeRenderer;
            }
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

        private async Task RenderFrameAsync(DrawingContext ctx)
        {
            // Console.WriteLine("RENDER FRAME");

            for (var i = 0; i < _panel.Gauges.Count; i++)
            {
                var gaugeRef = _panel.Gauges[i];

                if (gaugeRef.Skip == true)
                    continue;

                if (gaugeRef.Gauge == null)
                    throw new Exception("Gauge is null");

                var layersToDraw = gaugeRef.Gauge.Layers.ToArray().Reverse().ToList();

                var gaugeRenderer = _gaugeRenderers[i];

                gaugeRenderer.DrawGaugeLayers(ctx, layersToDraw, gaugeRef.Gauge, gaugeRef);

                // if (!isConnected)
                //     DrawDebugText(ctx, "Not connected", Brushes.Red, new Point(0, 0));
            }
        }

        // public async Task Render(bool isConnected)
        // {
        // var width = _window.Width;
        // var height = _window.Height;

        // var target = new RenderTargetBitmap(new PixelSize((int)width, (int)height));

        // using (var ctx = target.CreateDrawingContext())
        // {
        //     for (var i = 0; i < _panel.Gauges.Count; i++)
        //     {
        //         var gaugeRef = _panel.Gauges[i];

        //         if (gaugeRef.Skip == true)
        //             continue;

        //         if (gaugeRef.Gauge == null)
        //             throw new Exception("Gauge is null");

        //         var layersToDraw = gaugeRef.Gauge.Layers.ToArray().Reverse().ToList();

        //         var gaugeRenderer = _gaugeRenderers[i];

        //         gaugeRenderer.DrawGaugeLayers(ctx, layersToDraw, gaugeRef.Gauge, gaugeRef);

        //         if (!isConnected)
        //             DrawDebugText(ctx, "Not connected", Brushes.Red, new Point(0, 0));
        //     }

        //     // force re-paint
        //     Dispatcher.UIThread.Post(() =>
        //     {
        //         _imageControl.Source = target;
        //         _imageControl.InvalidateVisual();
        //     });
        // }
        // }

        public void Dispose()
        {
            _svgCache.Dispose();
            _window?.Close();

            GC.SuppressFinalize(this);
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
