using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    public enum TextHorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum TextVerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes what kind of text to render in the layer.
    /// </summary>
    public class TextDef
    {
        [JsonConverter(typeof(SimVarConfigConverter))]
        /// <summary>
        /// How to subscribe to a SimVar (and its unit) as the source of the text. eg. ["AIRSPEED INDICATED", "knots"]
        /// SimConnect note: all vars are requested as floats so units like "position" -127..127 are mapped to -1..1.
        /// <type>[string, string]</type>
        /// </summary>
        public SimVarConfig? Var { get; set; }
        /// <summary>
        /// The default text to render when there is no value.
        /// </summary>
        public string? Default { get; set; }
        /// <summary>
        /// How to format the text. [Cheatsheet](https://gist.github.com/luizcentennial/c6353c2ae21815420e616a6db3897b4c)
        /// </summary>
        public string? Template { get; set; }
        /// <summary>
        /// The size of the text.
        /// </summary>
        public double FontSize { get; set; } = 64;
        /// <summary>
        /// The family of the text. Supports any system font plus any inside the `fonts/` directory (currently only "Gordon").
        /// If you specify a font path this lets you choose a family inside it.
        /// <default>OS default ("Segoe UI" on Windows)<default>
        /// </summary>
        public string? FontFamily { get; set; }
        /// <summary>
        /// Path to a font file to use. Relative to the config JSON file.
        /// </summary>
        public string? Font { get; set; }
        [JsonConverter(typeof(ColorDefConverter))]
        /// <summary>
        /// The color of the text as a CSS-like value.
        /// eg. "rgb(255, 255, 255)" or "#FFF" or "white"
        /// <default>rgb(255, 255, 255)</default>
        /// </summary>
        public ColorDef? Color { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        /// <summary>
        /// How to align the text horizontally.
        /// "Left" would be the text starts at the layer X position, going right.
        /// "Right" would be the text starts at the layer X position, going left.
        /// <default>Center</default>
        /// </summary>
        public TextHorizontalAlignment Horizontal { get; set; } = TextHorizontalAlignment.Center;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        /// <summary>
        /// How to align the text vertically.
        /// "Top" would be the text starts at the layer Y position, going down.
        /// "Bottom" would be the text starts at the layer Y position, going up.
        /// <default>Center</default>
        /// </summary>
        public TextVerticalAlignment Vertical { get; set; } = TextVerticalAlignment.Center;
    }
}