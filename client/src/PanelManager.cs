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
            bool? lastIsConnected = null;

            while (true)
            {
                if (lastIsConnected == false && client.IsConnected)
                    Console.WriteLine("[PanelManager] Unpaused");

                lastIsConnected = client.IsConnected;
                
                if (client.IsConnected)
                {
                    await RenderPanels(config);
                }
                else
                {
                    Console.WriteLine("[PanelManager] Paused");

                    while (!client.IsConnected)
                        await Task.Delay(500);
                }

                await Task.Delay(1000 / config.Fps);
            }
        }
        
        async Task RenderPanels(Config config)
        {
            foreach (var (panel, renderer) in _panelsAndRenderers)
            {
                if (panel.Skip == true)
                    continue;
                    
                var width = panel.Width ?? renderer._window.Width;
                var height = panel.Height ?? renderer._window.Height;

                var target = new RenderTargetBitmap(new PixelSize((int)width, (int)height), new Vector(96, 96));

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
                    }

                    // force re-paint
                    Dispatcher.UIThread.Post(() =>
                    {
                        renderer._imageControl.Source = target;
                        renderer._imageControl.InvalidateVisual();
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
