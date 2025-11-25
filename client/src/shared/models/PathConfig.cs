using System.Text.Json.Serialization;

namespace OpenGaugeClient
{

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how a layer should translate along a path. Inherits from `TransformConfig`.
    /// </summary>
    public class PathConfig : TransformConfig
    {
        /// <summary>
        /// The path to an SVG to use. It must contain a single path element.
        /// </summary>
        public required string Image { get; set; }
        /// <summary>
        /// The width of the SVG (in pixels).
        /// <default>SVG viewbox width or layer width</default>
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// The width of the SVG (in pixels).
        /// <default>SVG viewbox height or layer height</default>
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
        /// The position of the image inside the gauge.
        /// X or Y can be a pixel value or a string which is a percent of the gauge.
        /// <type>[double|string, double|string]</type>
        /// <default>["50%", "50%"]</default>
        /// </summary>
        public FlexibleVector2 Position { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };

        public override string ToString()
        {
            return $"PathConfig(" +
                base.ToString() +
                $"Image={Image}," +
                $"Width={Width}," +
                $"Height={Height}," +
                $"Origin={Origin}," +
                $"Position={Position}" +
                ")";
        }
    }
}