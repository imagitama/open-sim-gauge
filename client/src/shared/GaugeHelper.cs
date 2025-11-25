namespace OpenGaugeClient
{
    public static class GaugeHelper
    {
        public static Gauge GetGaugeByName(string name)
        {
            var gauge = ConfigManager.Config!.Gauges.Find(gauge => gauge.Name == name);

            if (gauge == null)
                throw new Exception($"Failed to get gauge by name: {name}");

            return gauge;
        }

        public static int GetIndexByName(string name)
        {
            var gaugeIndex = ConfigManager.Config!.Gauges.FindIndex(gauge => gauge.Name == name);

            if (gaugeIndex == -1)
                throw new Exception($"Failed to get gauge index by name: {name}");

            return gaugeIndex;
        }

        public static async Task<Gauge> GetGaugeByPath(string path)
        {
            var absolutePath = PathHelper.GetFilePath(path, forceToGitRoot: true);

            var gauge = await JsonHelper.LoadTypedJson<Gauge>(absolutePath);

            if (gauge == null)
                throw new Exception($"Failed to get gauge by path: {path}");

            gauge.Source = absolutePath;

            return gauge;
        }

        public static async Task SaveGaugeToFile(Gauge gaugeToSave)
        {
            if (gaugeToSave.Source == null)
                throw new Exception("Cannot save without a source");

            var jsonPath = gaugeToSave.Source;

            Console.WriteLine($"[GaugeHelper] Save gauge to file gauge={gaugeToSave} path={jsonPath}");

            await JsonHelper.SaveJson(gaugeToSave, jsonPath);
        }

        public static List<Gauge> FindGaugesReferencedByPathInAllPanels()
        {
            var gauges = ConfigManager.Config.Panels.SelectMany(panel => panel.Gauges.Where(g => g.Gauge?.Source != null).Select(g => g.Gauge)).ToList();
            return gauges as List<Gauge>;
        }
    }
}