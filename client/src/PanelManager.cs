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

        public PanelManager()
        {
            _gaugeCache = new GaugeCache();
        }

        public void Initialize(Config config, Func<string, string, object?> _getSimVarValue)
        {
            foreach (var panel in config.Panels)
            {
                if (panel.Skip == true)
                {
                    Console.WriteLine($"[PanelManager] Skipping panel '{panel.Name}'");
                    continue;
                }

                var renderer = new PanelRenderer(
                    panel,
                    _getSimVarValue
                );

                _panelsAndRenderers.Add((panel, renderer));
            }
        }

        public async Task RunRenderLoop(Config config)
        {
            while (true)
            {
                foreach (var (panel, renderer) in _panelsAndRenderers)
                {
                    if (panel.Skip == true)
                        continue;
                        
                    var width = panel.Width ?? (int)renderer._window.Width;
                    var height = panel.Height ?? (int)renderer._window.Height;

                    var target = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));

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

                        // TODO: move into renderer?
                        Dispatcher.UIThread.Post(() =>
                        {
                            renderer._imageControl.Source = target;
                            renderer._imageControl.InvalidateVisual();
                        });
                    }
                }

                await Task.Delay(1000 / config.Fps ?? 30);
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
