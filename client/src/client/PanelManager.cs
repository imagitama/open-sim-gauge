namespace OpenGaugeClient.Client
{
    public class PanelManager : IDisposable
    {
        private readonly Dictionary<string, PanelRenderer> _panelRenderers = [];
        private readonly ImageCache _imageCache;
        private readonly SKFontCache _fontCache;
        private readonly SKFontProvider _skFontProvider;
        private readonly FontProvider _fontProvider;
        private readonly SvgCache _svgCache;

        public PanelManager()
        {
            _fontCache = new SKFontCache();
            _skFontProvider = new SKFontProvider(_fontCache);
            _fontProvider = new FontProvider();
            _svgCache = new SvgCache();
            _imageCache = new ImageCache(_skFontProvider);
        }

        public void Initialize(Config config, Func<string, string, double?> _getSimVarValue, string? vehicleName)
        {
            Console.WriteLine($"Initializing panels...{(vehicleName != null ? $" (vehicle '{vehicleName}')" : "")}");

            foreach (var panel in config.Panels)
            {
                var key = panel.Name;

                if (!PanelHelper.GetIsPanelVisible(panel, vehicleName))
                {
                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"[PanelManager] Panel should not be visible currentVehicle={vehicleName} panelVehicle={(panel.Vehicle != null ? string.Join(",", panel.Vehicle) : "null")} panel={panel}");

                    UnrenderPanel(panel);
                    continue;
                }

                if (panel.Skip == true)
                {
                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"[PanelManager] Skipping panel={panel}'");

                    UnrenderPanel(panel);
                    continue;
                }

                if (_panelRenderers.ContainsKey(key))
                {
                    _panelRenderers[key].Show();

                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"[PanelManager] Show panel={panel}'");

                    return;
                }

                Console.WriteLine($"Panel '{panel.Name}' (vehicle: {(panel.Vehicle != null ? string.Join(", ", panel.Vehicle) : "all")})");

                var renderer = new PanelRenderer(
                    panel,
                    _imageCache,
                    _fontProvider,
                    _svgCache,
                    _getSimVarValue
                );

                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[PanelManager] Load panel={panel}");

                _panelRenderers[key] = renderer;
            }

            if (_panelRenderers.Count == 0)
                Console.WriteLine("No panels were initialized");
        }

        private void UnrenderPanel(Panel panel)
        {
            var key = panel.Name;

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[PanelManager] Unrender panel={panel}");

            if (_panelRenderers.ContainsKey(key))
            {
                if (panel.KeepAlive == true)
                {
                    _panelRenderers[key].Hide();

                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"[PanelManager] Hide panel={panel}'");
                }
                else
                {
                    _panelRenderers[key].Dispose();
                    _panelRenderers.Remove(key);

                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"[PanelManager] Unrendered panel={panel}'");
                }
            }
        }

        public void SetConnected(bool isConnected)
        {
            foreach (var (name, renderer) in _panelRenderers)
                renderer.SetConnected(isConnected);
        }

        public void Dispose()
        {
            foreach (var renderer in _panelRenderers.Values)
                renderer.Dispose();
            _panelRenderers.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
