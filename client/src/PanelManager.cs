using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace OpenGaugeClient
{
    public class PanelManager : IDisposable
    {
        public bool IsReady = false;
        private readonly Dictionary<string, PanelRenderer> _panelRenderers = [];
        private readonly GaugeCache _gaugeCache;
        private readonly ImageCache _imageCache;
        private readonly FontCache _fontCache;
        private readonly FontProvider _fontProvider;
        private readonly SvgCache _svgCache;

        public PanelManager()
        {
            _gaugeCache = new GaugeCache();
            _fontCache = new FontCache();
            _fontProvider = new FontProvider(_fontCache);
            _svgCache = new SvgCache();
            _imageCache = new ImageCache(_fontProvider);
        }

        public void Initialize(Config config, Func<string, string, object?> _getSimVarValue, string? vehicleName)
        {
            if (config.Debug == true)
                Console.WriteLine($"[PanelManager] Initialize '{vehicleName}'");

            if (vehicleName == null)
                return;

            foreach (var panel in config.Panels)
            {
                if (panel.Vehicle != null && vehicleName != null && !Utils.GetIsVehicle(panel.Vehicle, vehicleName))
                {
                    UnrenderPanel(panel);
                    continue;
                }

                if (panel.Skip == true)
                {
                    Console.WriteLine($"Skipping panel '{panel.Name}'");
                    UnrenderPanel(panel);
                    continue;
                }

                if (_panelRenderers.ContainsKey(panel.Name))
                    continue;

                var renderer = new PanelRenderer(
                    panel,
                    _imageCache,
                    _fontProvider,
                    _svgCache,
                    _getSimVarValue
                );

                Console.WriteLine($"[PanelManager] Load panel '{panel.Name}' {panel.Width}x{panel.Height} screen={panel.Screen} fullscreen={panel.Fullscreen}");

                _panelRenderers[panel.Name] = renderer;
            }

            IsReady = true;
        }

        private void UnrenderPanel(Panel panel)
        {
            if (_panelRenderers.ContainsKey(panel.Name))
            {
                _panelRenderers[panel.Name].Dispose();
                _panelRenderers.Remove(panel.Name);

                Console.WriteLine($"[PanelManager] Unload panel '{panel.Name}'");
            }
        }

        public async Task RunRenderLoop(Config config, Client client)
        {
            Console.WriteLine("Rendering panels...");

            bool? lastIsConnected = null;

            while (true)
            {
                if (client.IsConnected)
                {
                    if (lastIsConnected == false)
                    {
                        Console.WriteLine("[PanelManager] Connection resumed");
                    }

                    lastIsConnected = client.IsConnected;
                }
                else
                {
                    if (lastIsConnected == true)
                    {
                        Console.WriteLine("[PanelManager] Connection lost");

                        lastIsConnected = client.IsConnected;

                        while (!client.IsConnected)
                            await Task.Delay(100);
                    }
                }

                await RenderPanels(config, client.IsConnected);

                await Task.Delay(1000 / config.Fps);
            }
        }

        async Task RenderPanels(Config config, bool isConnected)
        {
            if (!IsReady)
                return;

            // ensure copy to avoid InvalidOperationException
            foreach (var (panelName, renderer) in _panelRenderers.ToList())
            {
                var panel = config.Panels.Find(panel => panel.Name == panelName) ?? throw new Exception($"Cannot render panel '{panelName}' - panel data not found");
                var width = renderer.Window.Width;
                var height = renderer.Window.Height;

                var target = new RenderTargetBitmap(new PixelSize((int)width, (int)height));

                using (var ctx = target.CreateDrawingContext())
                {
                    foreach (var gaugeRef in panel.Gauges)
                    {
                        if (gaugeRef.Skip == true)
                            continue;

                        Gauge? gauge;

                        if (gaugeRef.Path != null)
                        {
                            gauge = await _gaugeCache.Load(gaugeRef.Path);
                        }
                        else
                        {
                            gauge = config.Gauges.Find(g => g.Name == gaugeRef.Name);
                        }

                        if (gauge == null)
                            throw new Exception($"[PanelManager] Gauge '{gaugeRef.Name ?? gaugeRef.Path}' not found");

                        var layersToDraw = gauge.Layers.ToArray().Reverse().ToList();

                        renderer.DrawGaugeLayers(ctx, layersToDraw, gauge, gaugeRef);

                        if (!isConnected)
                            PanelRenderer.DrawDebugText(ctx, "Not connected", Brushes.Red, new Point(0, 0));
                    }

                    // force re-paint
                    Dispatcher.UIThread.Post(() =>
                    {
                        renderer.ImageControl.Source = target;
                        renderer.ImageControl.InvalidateVisual();
                    });
                }
            }
        }

        public void Dispose()
        {
            foreach (var (panelName, renderer) in _panelRenderers)
                renderer.Dispose();
            _panelRenderers.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
