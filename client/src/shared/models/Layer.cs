using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes a layer of a gauge.
    /// </summary>
    public class Layer
    {
        /// <summary>
        /// The name of the layer. Only used for debugging.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Some text to render as this layer. If provided then `image` will be ignored.
        /// </summary>
        public TextDef? Text { get; set; }
        /// <summary>
        /// A path to an image to render as this layer. PNG and SVG supported.
        /// </summary>
        public string? Image { get; set; }
        [JsonConverter(typeof(FlexibleDimensionConverter))]
        /// <summary>
        /// The width of the layer (in pixels).
        /// <default>Gauge width</default>
        /// </summary>
        public FlexibleDimension? Width { get; set; }
        [JsonConverter(typeof(FlexibleDimensionConverter))]
        /// <summary>
        /// The height of the layer (in pixels).
        /// <default>Gauge height</default>
        /// </summary>
        public FlexibleDimension? Height { get; set; }
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The origin of the layer (in pixels) for all transformations to be based on.
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
        /// The position of the layer inside the gauge.
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
        /// How to transform this layer using a SimVar.
        /// </summary>
        public TransformDef? Transform { get; set; }
        /// <summary>
        /// How many degrees to initially rotate the layer.
        /// </summary>
        public double Rotate { get; set; } = 0;
        /// <summary>
        /// How much to initially translate the layer on the X axis.
        /// </summary>
        public double TranslateX { get; set; } = 0;
        /// <summary>
        /// How much to initially translate the layer on the Y axis.
        /// </summary>
        public double TranslateY { get; set; } = 0;
        /// <summary>
        /// A color to fill with (behind any image you specify).
        /// </summary>
        [JsonConverter(typeof(ColorDefConverter))]
        public ColorDef? Fill { get; set; }
        /// <summary>
        /// Render useful debugging visuals such as bounding box.
        /// Note: If you subscribe to a SimVar in this layer and debugging is enabled it is sent to the server for extra logging.
        /// </summary>
        public bool Debug { get; set; } = false;
        /// <summary>
        /// If to skip rendering this layer.
        /// </summary>
        public bool Skip { get; set; } = false;

        public Layer Clone()
        {
            return new Layer()
            {
                Name = Name,
                Text = Text,
                Image = Image,
                Width = Width,
                Height = Height,
                Origin = Origin,
                Position = Position,
                Transform = Transform,
                Rotate = Rotate,
                TranslateX = TranslateX,
                TranslateY = TranslateY,
                Debug = Debug,
                Skip = Skip
            };
        }
        public override string ToString()
        {
            return $"Layer(" +
                   $"Name={Name ?? "null"}," +
                   $"Image={Image ?? "null"}," +
                   $"Width={Width?.ToString() ?? "null"}," +
                   $"Height={Height?.ToString() ?? "null"}," +
                   $"Origin={Origin}," +
                   $"Position={Position}," +
                   $"Transform={Transform}," +
                   $"Debug={Debug}," +
                   $"Skip={Skip}" +
            ")";
        }
    }
}