using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how to clip the layers of a gauge.
    /// </summary>
    public class ClipConfig
    {
        /// <summary>
        /// The path to the SVG to use to clip. It must contain a single path element (such as a circle).
        /// </summary>
        public required string Image { get; set; }
        /// <summary>
        /// The width of the SVG (in pixels).
        /// <default>SVG viewbox width or gauge width</default>
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// The width of the SVG (in pixels).
        /// <default>SVG viewbox height or gauge height</default>
        /// </summary>
        public double? Height { get; set; }
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The origin of the SVG for positioning.
        /// <type>[double|string, double|string]</type>
        /// <default>["50%", "50%"]</default>
        /// </summary>
        public FlexibleVector2 Origin { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The position of the clip inside the gauge.
        /// X or Y can be a pixel value or a string which is a percent of the gauge.
        /// <type>[double|string, double|string]</type>
        /// <default>["50%", "50%"]</default>
        /// </summary>
        public FlexibleVector2 Position { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };
        /// <summary>
        /// Extra debugging for clipping.
        /// </summary>
        public bool Debug { get; set; } = false;

        public override string ToString()
        {
            return $"ClipConfig(" +
                   $"Image={Image ?? "null"}'," +
                   $"Width={Width?.ToString() ?? "null"}," +
                   $"Height={Height?.ToString() ?? "null"}," +
                   $"Origin={Origin}," +
                   $"Position={Position}," +
                   $"Debug={Debug}" +
                ")";
        }
    }
}