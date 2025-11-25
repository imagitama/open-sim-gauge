using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes a gauge and how to render it.
    /// </summary>
    public class Gauge
    {
        /// <summary>
        /// The name of the gauge. Used for referencing it from a panel and for debugging.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Replace this gauge with another gauge in another file. Does not merge anything.
        /// </summary>
        public string? Path { get; set; }
        /// <summary>
        /// The width of the gauge (in pixels).
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// The height of the gauge (in pixels).
        /// </summary>
        public int Height { get; set; }
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The origin of the gauge which is used for all transforms such as positioning, scaling and rotation.
        /// <type>[double|string, double|string]</type>
        /// <default>["50%", "50%"]</default>
        /// </summary>
        public FlexibleVector2 Origin { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };
        /// <summary>
        /// The layers to render to make the gauge.
        /// </summary>
        public List<Layer> Layers { get; set; } = new();
        /// <summary>
        /// How to clip the layers of the gauge. Useful for gauges like an attitude indicator that translates outside of the gauge bounds.
        /// </summary>
        public ClipConfig? Clip { get; set; }
        /// <summary>
        /// Renders a grid with the provided cell size.
        /// </summary>
        public double? Grid { get; set; }
        /// <summary>
        /// Extra logging. Beware of console spam!
        /// </summary>
        public bool? Debug { get; set; }

        public override string ToString()
        {
            return $"Gauge(" +
                   $"Name={Name ?? "null"}," +
                   $"Width={Width.ToString() ?? "null"}," +
                   $"Height={Height.ToString() ?? "null"}," +
                   $"Origin={Origin}," +
                   $"Layers=\n{string.Join("\n", Layers.Select(l => $"  {l}"))}\n," +
                   $"Clip={Clip}," +
                   $"Grid={Grid}," +
                   $"Source={Source}" +
                ")";
        }

        public void Replace(Gauge newGauge)
        {
            Name = newGauge.Name;
            Path = newGauge.Path;
            Width = newGauge.Width;
            Height = newGauge.Height;
            Origin = newGauge.Origin;
            Layers = newGauge.Layers;
            Clip = newGauge.Clip;
        }

        [JsonIgnore]
        /// <summary>
        /// An absolute path to the source JSON file. Do not write anywhere.
        /// </summary>
        public string? Source { get; set; }
    }
}