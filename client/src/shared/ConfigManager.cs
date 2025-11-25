namespace OpenGaugeClient
{
    public static class ConfigManager
    {
        private static Config? _config;
        public static Config Config => _config ?? throw new InvalidOperationException("LoadConfig must be called first");

        public static async Task SavePanel(int panelIndex, Panel panelToSave)
        {
            Config.Panels[panelIndex] = panelToSave;

            await SaveConfig();
        }

        public static async Task AddPanel(Panel panelToAdd)
        {
            Config.Panels.Add(panelToAdd);

            await SaveConfig();
        }

        public static async Task DeletePanel(int panelIndex)
        {
            Config.Panels.RemoveAt(panelIndex);

            await SaveConfig();
        }

        public static async Task SaveGauge(int gaugeIndex, Gauge gaugeToSave)
        {
            Console.WriteLine($"[ConfigManager] Save gauge index={gaugeIndex} gauge={gaugeToSave}");

            Config!.Gauges[gaugeIndex] = gaugeToSave;

            await SaveConfig();
        }

        public static async Task AddGauge(Gauge gaugeToAdd)
        {
            Config.Gauges.Add(gaugeToAdd);

            await SaveConfig();
        }

        public static async Task DeleteGauge(int panelIndex)
        {
            Config.Gauges.RemoveAt(panelIndex);

            await SaveConfig();
        }

        public static async Task<Config> LoadConfig(string? overridePath = null)
        {
            var configPath = overridePath ?? PathHelper.GetFilePath("client.json", forceToGitRoot: false);

            Console.WriteLine($"Load config: {configPath}");

            var newConfig = await JsonHelper.LoadTypedJson<Config>(configPath);

            if (_config?.Debug == true || newConfig.Debug)
                Console.WriteLine($"[ConfigManager] Loaded config from {configPath}");

            var _gaugeCache = new GaugeCache();

            foreach (var panel in newConfig.Panels)
            {
                foreach (var gaugeRef in panel.Gauges)
                {
                    Gauge? gauge;

                    if (gaugeRef.Path != null)
                    {
                        gauge = await _gaugeCache.Load(gaugeRef.Path);
                    }
                    else
                    {
                        gauge = newConfig.Gauges.Find(g => g.Name == gaugeRef.Name);
                    }

                    if (gauge == null)
                        Console.WriteLine($"Panel '{panel.Name}' has invalid gauge '{gaugeRef.Name}' or path '{gaugeRef.Path}'");

                    gaugeRef.Gauge = gauge;
                }
            }

            for (var i = 0; i < newConfig.Gauges.Count; i++)
            {
                var gauge = newConfig.Gauges[i];

                if (gauge.Path != null)
                {
                    var path = gauge.Path;

                    var newGauge = await _gaugeCache.Load(path);

                    gauge.Replace(newGauge);

                    newGauge.Source = path;

                    newConfig.Gauges[i] = newGauge;
                }
            }

            _config = newConfig;

            return newConfig;
        }

        public static async Task SaveConfig()
        {
            await JsonHelper.SaveJson(Config, "client.json", false);
        }
    }
}