namespace OpenGaugeClient.Client
{
    public class PanelManager : IDisposable
    {
        private bool _isInitialized = false;
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
            Console.WriteLine($"Initializing {config.Panels.Count} panels... (vehicle = {vehicleName})");

            if (_isInitialized)
                Uninitialize();

            if (vehicleName == null)
                return;

            foreach (var panel in config.Panels)
            {
                if (panel.Vehicle != null && vehicleName != null && !Utils.GetIsVehicle(panel.Vehicle, vehicleName) && ConfigManager.Config.Debug != true)
                {
                    if (ConfigManager.Config.Debug)
                        Console.WriteLine($"[PanelManager] Panel vehicle={panel.Vehicle} does not match provided={vehicleName}");

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

                if (_panelRenderers.ContainsKey(panel.Name))
                    continue;

                var renderer = new PanelRenderer(
                    panel,
                    _gaugeCache,
                    _imageCache,
                    _fontProvider,
                    _svgCache,
                    _getSimVarValue
                );

                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[PanelManager] Load panel={panel}");

                _panelRenderers[panel.Name] = renderer;
            }

            _isInitialized = true;
        }

        private void Uninitialize()
        {
            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[PanelManager] Already initialized, uninitializing...");

            Dispose();
            _isInitialized = false;
        }

        private void UnrenderPanel(Panel panel)
        {
            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[PanelManager] Unrender panel={panel}");

            if (_panelRenderers.ContainsKey(panel.Name))
            {
                _panelRenderers[panel.Name].Dispose();
                _panelRenderers.Remove(panel.Name);

                if (ConfigManager.Config.Debug)
                    Console.WriteLine($"[PanelManager] Unrendered panel={panel}'");
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
