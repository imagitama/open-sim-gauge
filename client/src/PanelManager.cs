using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace OpenGaugeClient
{
    public class PanelManager : IDisposable
    {
        private readonly List<(Panel panel, PanelRenderer renderer)> _panelsAndRenderers = new();
        private GaugeCache _gaugeCache;
        private ImageCache _imageCache;
        private FontCache _fontCache;
        private FontProvider _fontProvider;
        private SvgCache _svgCache;

        public PanelManager()
        {
            _gaugeCache = new GaugeCache();
            _fontCache = new FontCache();
            _fontProvider = new FontProvider(_fontCache);
            _svgCache = new SvgCache();
            _imageCache = new ImageCache(_fontProvider);
        }

        public void Initialize(Config config, Func<string, string, object?> _getSimVarValue)
        {
            foreach (var panel in config.Panels)
            {
                if (panel.Skip == true)
                {
                    Console.WriteLine($"Skipping panel '{panel.Name}'");
                    continue;
                }

                var renderer = new PanelRenderer(
                    panel,
                    _imageCache,
                    _fontProvider,
                    _svgCache,
                    _getSimVarValue
                );

                _panelsAndRenderers.Add((panel, renderer));
            }
        }

        public async Task RunRenderLoop(Config config, Client client)
        {
            Console.WriteLine("Rendering panels...");
            
            bool? lastIsConnected = null;

            while (true)
            {
                if (lastIsConnected == false && client.IsConnected)
                    Console.WriteLine("[PanelManager] Connection resumed");

                await RenderPanels(config, client.IsConnected);
                
                if (!client.IsConnected)
                {
                    Console.WriteLine("[PanelManager] Connection lost");

                    while (!client.IsConnected)
                        await Task.Delay(500);
                }
                
                lastIsConnected = client.IsConnected;

                await Task.Delay(1000 / config.Fps);
            }
        }
        
        async Task RenderPanels(Config config, bool isConnected)
        {
            foreach (var (panel, renderer) in _panelsAndRenderers)
            {
                if (panel.Skip == true)
                    continue;
                
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
            foreach (var (panel, renderer) in _panelsAndRenderers)
                renderer.Dispose();
            _panelsAndRenderers.Clear();
        }
    }
}
