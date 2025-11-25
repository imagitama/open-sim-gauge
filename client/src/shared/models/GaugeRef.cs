using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes a reference to a gauge to render inside a panel.
    /// </summary>
    public class GaugeRef
    {
        /// <summary>
        /// The name of the gauge to use. Optional if you specify a path.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// The path to a JSON file that contains the gauge to use.
        /// The file should contain a single property "gauge" which is the Gauge object.
        /// </summary>
        public string? Path { get; set; }
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The position of the gauge inside the panel.
        /// X or Y can be a pixel value or a string which is a percent of the panel.
        /// Use a negative value to flip the position (so -100 is 100px from the right edge).
        /// <type>[double|string, double|string]</type>
        /// <default>[0, 0]</default>
        /// </summary>
        public FlexibleVector2 Position { get; set; } = new FlexibleVector2()
        {
            X = "50%",
            Y = "50%"
        };
        /// <summary>
        /// How much to scale the gauge (respecting the width you set).
        /// </summary>
        public double Scale { get; set; } = 1.0;
        /// <summary>
        /// Force the width (and height) of the gauge in pixels before scaling.
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// If to skip rendering this gauge.
        /// </summary>
        public bool Skip { get; set; } = false;

        public bool Equals(GaugeRef otherGaugeRef)
        {
            return Name != null && otherGaugeRef.Name == Name || Path != null && otherGaugeRef.Path == Path;
        }

        [JsonIgnore]
        public string Label
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name;

                if (!string.IsNullOrEmpty(Gauge?.Name))
                    return Gauge.Name;

                if (!string.IsNullOrEmpty(Path))
                    return System.IO.Path.GetFileNameWithoutExtension(Path) ?? string.Empty;

                return string.Empty;
            }
        }

        public GaugeRef Clone()
        {
            return new GaugeRef
            {
                Name = Name,
                Path = Path,
                Position = new FlexibleVector2
                {
                    X = Position.X,
                    Y = Position.Y
                },
                Scale = Scale,
                Width = Width,
                Skip = Skip,
                Gauge = Gauge
            };
        }

        public override string ToString()
        {
            return $"GaugeRef(" +
                   $"Name={Name ?? "null"}," +
                   $"Path={Path ?? "null"}," +
                   $"Position={Position}," +
                   $"Scale={Scale}," +
                   $"Width={Width}," +
                   $"Skip={Skip}" +
                ")";
        }

        [JsonIgnore]
        public Gauge? Gauge;
    }
}