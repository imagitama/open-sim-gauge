namespace OpenGaugeClient
{
    [GenerateMarkdownTable]
    /// <summary>
    /// The config for your client.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// The intended FPS when rendering.
        /// </summary>
        public int Fps { get; set; } = 60;
        /// <summary>
        /// Configure the server IP and port.
        /// <default></default>
        /// </summary>
        public ServerConfig Server { get; set; } = new();
        /// <summary>
        /// The panels to render. On desktop a panel is a window.
        /// </summary>
        public required List<Panel> Panels { get; set; }
        /// <summary>
        /// The gauges that are available to your panels. Optional because your panels can reference gauge JSON files by path.
        /// <default>[]</default>
        /// </summary>
        public List<Gauge> Gauges { get; set; } = new();
        /// <summary>
        /// Log extra info to the console.
        /// </summary>
        public bool Debug { get; set; } = false;
        /// <summary>
        /// If to only render panels if connected.
        /// Note: There should always be a console open on launch.
        /// </summary>
        public bool RequireConnection { get; set; } = true;

        // internal
        public Gauge GetGauge(int? rootLevelIndex, string? gaugePath)
        {
            if (rootLevelIndex != null)
            {
                if (rootLevelIndex < Gauges.Count)
                {
                    return Gauges[(int)rootLevelIndex]!;
                }
                else
                {
                    throw new Exception($"Could not get gauge by index {rootLevelIndex}");
                }
            }
            if (gaugePath != null)
            {
                var gaugeRef = Panels.Select(panel => panel.Gauges.Find(gaugeRef => gaugeRef.Path == gaugePath)).First();

                if (gaugeRef == null)
                    throw new Exception($"Could not get gauge by path '{gaugePath}'");

                return gaugeRef.Gauge!;
            }
            throw new Exception("Need an index or path");
        }
    }
}