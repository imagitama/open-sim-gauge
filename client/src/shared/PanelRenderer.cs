using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Input;

namespace OpenGaugeClient
{
    public class PanelRenderer : IDisposable
    {
        private Panel _panel;
        private readonly ImageCache _imageCache;
        private readonly FontProvider _fontProvider;
        private readonly SvgCache _svgCache;
        private Func<string, string, double?> _getSimVarValue { get; set; }
        private readonly Dictionary<int, GaugeRenderer> _gaugeRenderers = [];
        private bool? _disableRenderOnTop = false;
        private bool? _isConnected = false;
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
        private int? _gridSize = null;
        private int? _debugGaugeIndex = null;
        public Action<PixelPoint>? OnMove;
        private bool _isDisposed = false;

        public PanelRenderer(
            Panel panel,
            ImageCache imageCache,
            FontProvider fontProvider,
            SvgCache svgCache,
            Func<string, string, double?> getSimVarValue,
            bool? isConnected = null,
            bool? disableRenderOnTop = false,
            int? gridSize = null
        )
        {
            _panel = panel;
            _imageCache = imageCache;
            _fontProvider = fontProvider;
            _svgCache = svgCache;
            _getSimVarValue = getSimVarValue;
            _isConnected = isConnected;
            _disableRenderOnTop = disableRenderOnTop;
            _gridSize = gridSize;

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

            var debug = ConfigManager.Config.Debug || panel.Debug == true;

            _window = PanelHelper.CreatePanelWindowFromPanel(_panel);
            _window.Content = canvas;

            if (disableRenderOnTop == true)
                _window.Topmost = false;

            if (ConfigManager.Config.Debug || panel.Debug == true)
            {
                _window.Cursor = new Cursor(StandardCursorType.SizeAll);

                _window.PointerPressed += (sender, e) =>
                {
                    if (e.GetCurrentPoint(_window).Properties.IsLeftButtonPressed)
                    {
                        _window.Cursor = new Cursor(StandardCursorType.SizeAll);
                        _window.BeginMoveDrag(e);
                    }
                };
                _window.PointerEntered += (_, e) =>
                {
                    _window.Cursor = new Cursor(StandardCursorType.SizeAll);
                };
                _window.PointerReleased += (_, e) =>
                {
                    _window.Cursor = new Cursor(StandardCursorType.SizeAll);
                };

                _window.PositionChanged += (_, _) =>
                {
                    var pos = _window.Position;
                    OnMove?.Invoke(pos);
                };
            }

            RebuildGaugeRenderers();

            _renderer = new RenderingHelper(_imageControl, RenderFrameAsync, ConfigManager.Config!.Fps, _window);

            _window.Opened += (_, _) => _renderer.Start();
            _window.Closing += (_, _) => _renderer.Dispose();

            _window.Show();
        }

        public void ReplacePanel(Panel newPanel)
        {
            _panel = newPanel;

            PanelHelper.UpdateWindowForPanel(_window, _panel);

            if (_disableRenderOnTop == true)
                _window.Topmost = false;

            RebuildGaugeRenderers();
        }

        public void UpdateGrid(int? gridSize)
        {
            _gridSize = gridSize;
        }

        public void DebugGauge(int? index)
        {
            _debugGaugeIndex = index;
        }

        public void SetConnected(bool isConnected)
        {
            _isConnected = isConnected;
        }

        void RebuildGaugeRenderers()
        {
            for (var i = 0; i < _panel.Gauges.Count; i++)
            {
                var gaugeRef = _panel.Gauges[i];

                var gauge = gaugeRef.Gauge;

                if (gauge == null)
                {
                    Console.WriteLine($"Panel '{_panel.Name}' has empty gauge at index {i}");
                    gauge = new Gauge()
                    {
                        Width = 200,
                        Height = 200,
                        Layers = new List<Layer>()
                        {
                            new Layer()
                            {
                                Text = new TextDef()
                                {
                                    Default = $"Gauge #{i} not found",
                                    Color = new ColorDef(255, 0, 0),
                                    FontSize = 16
                                }
                            }
                        }
                    };
                }

                var gaugeRenderer = new GaugeRenderer(
                    gauge,
                    gaugeRef,
                    (int)_window.Width,
                    (int)_window.Height,
                    renderScaling: _window.RenderScaling,
                    _imageCache,
                    _fontProvider,
                    _svgCache,
                    _getSimVarValue,
                    debug: _debugGaugeIndex != null && _debugGaugeIndex == i
                );

                _gaugeRenderers[i] = gaugeRenderer;
            }
        }

        private async Task RenderFrameAsync(DrawingContext ctx)
        {
            if (_isDisposed)
                return;

            var gridSize = _gridSize != null && _gridSize > 0 ? _gridSize : _panel.Grid != null && _panel.Grid > 0 ? _panel.Grid : null;

            if (gridSize != null)
                RenderingHelper.DrawGrid(ctx, (int)_window.Width, (int)_window.Height, (double)gridSize);

            if (_panel.Debug == true || ConfigManager.Config.Debug == true)
                DrawPanelDebugInfo(ctx);

            if (_isConnected == false)
                DrawDebugText(ctx, "Not connected", Brushes.Red, new Point(0, 0));

            for (var i = 0; i < _panel.Gauges.Count; i++)
            {
                var gaugeRef = _panel.Gauges[i];

                if (gaugeRef.Skip == true)
                    continue;

                var gaugeRenderer = _gaugeRenderers[i];

                if (gaugeRenderer != null)
                    gaugeRenderer.DrawGaugeLayers(ctx, disableClipping: _panel.Clip == false);
            }
        }

        public static string GetScreenSummary(Window w)
        {
            var screen = w.Screens.ScreenFromWindow(w);
            if (screen == null)
                return "unknown 0x0 1.0";

            var b = screen.Bounds;
            string screenId = $"{b.X},{b.Y}";

            return $"Screen {screenId} {b.Width}x{b.Height} {screen.Scaling:0.##}x";
        }

        private void DrawPanelDebugInfo(DrawingContext ctx)
        {
            var canvasWidth = (int)_window!.Width;
            var canvasHeight = (int)_window!.Height;

            var formattedText = new FormattedText(
                $"'{_panel.Name}'" + (_panel.Vehicle != null ? $" (vehicle={string.Join(",", _panel.Vehicle)})" : "") + "\n" +
                $"{_panel.Position.X},{_panel.Position.Y} => {_window.Position.X},{_window.Position.Y}\n" +
                $"{_panel.Width}x{_panel.Height} => {_window.Width}x{_window.Height}\n" +
                GetScreenSummary(_window),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                14,
                Brushes.White
            );

            var x = canvasWidth - formattedText.Width;
            var y = canvasHeight - formattedText.Height;

            ctx.DrawText(formattedText, new Point(x - 10, y - 10));
        }

        public void Dispose()
        {
            _isDisposed = true;

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
