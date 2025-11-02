using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    public static class ConfigManager
    {
        public static Config? Config { get; private set; }

        public static async Task<Config> LoadConfig()
        {
            return await LoadJson<Config>("config.json", true);
        }

        public static async Task<T> LoadJson<T>(string filePath, bool useDevRoot = false)
        {
            string absoluteFilePath = PathHelper.GetFilePath(filePath, useDevRoot);

            if (Config?.Debug == true)
                Console.WriteLine($"Load JSON: {absoluteFilePath}");

            if (!File.Exists(absoluteFilePath))
                throw new Exception($"File not found: {absoluteFilePath}");

            string json = await File.ReadAllTextAsync(absoluteFilePath);

            try
            {
                var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var result = JsonSerializer.Deserialize<T>(ref reader, options);
                return result!;
            }
            catch
            {
                Console.WriteLine(json);
                throw;
            }
        }

        public static bool Debug => Config?.Debug == true;
    }

    [GenerateMarkdownTable]
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
    }

    [GenerateMarkdownTable]
    public class ServerConfig
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 1234;
    }

    [GenerateMarkdownTable]
    public class Panel
    {
        /// <summary>
        /// The unique name of this panel.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The name of a vehicle (ie. aircraft) to only render this panel for. Wildcards supported (eg. `"*Skyhawk*"`).
        /// Uses aircraft title eg. "Cessna Skyhawk G1000 Asobo".        
        /// </summary>
        public string? Vehicle { get; set; }
        /// <summary>
        /// Which gauges to render in this panel.
        /// </summary>
        public required List<GaugeRef> Gauges { get; set; }
        /// <summary>
        /// If to skip rendering this panel.
        /// </summary>
        public bool? Skip { get; set; } = false;
        /// <summary>
        /// The index of the screen you want to render this panel on (starting at 0 which is usually your main one).
        /// </summary>
        public int? Screen { get; set; } = 0;
        /// <summary>
        /// The width of the panel in pixels or a percent of the screen.
        /// Optional if you use fullscreen.
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// The width of the panel in pixels or a percent of the screen.
        /// Optional if you use fullscreen.
        /// </summary>
        public double? Height { get; set; }
        /// <summary>
        /// If to have the panel fill the screen.
        /// </summary>
        public bool Fullscreen { get; set; } = false;
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The position of the panel inside the screen. X or Y can be a pixel value or a string which is a percent of the screen.
        /// Use a negative value to flip the position (so -100 is 100px from the right edge).
        /// <type>[double|string, double|string]</type>
        /// <default>["50%", "50%"]</default>
        /// </summary>
        public FlexibleVector2 Position { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The origin of the panel which is used for all transforms such as positioning, scaling and rotation.
        /// <type>[double|string, double|string]</type>
        /// <default>["50%", "50%"]</default>
        /// </summary>
        public FlexibleVector2 Origin { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };
        [JsonConverter(typeof(ColorDefConverter))]
        /// <summary>
        /// Background color of the panel as a CSS-like value. Cannot use transparency.
        /// eg. "rgb(255, 255, 255)" or "#FFF" or "white"
        /// <type>string</type>
        /// <default>rgb(0, 0, 0)</default>
        /// </summary>
        public ColorDef? Background { get; set; } = new ColorDef(0, 0, 0);
        /// <summary>
        /// If to render with a transparent background.
        /// </summary>
        public bool? Transparent { get; set; } = false;
        /// <summary>
        /// Extra console logging for this panel.
        /// </summary>
        public bool? Debug { get; set; } = false;
    }

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
        public FlexibleVector2 Position { get; set; } = new();
        /// <summary>
        /// How much to scale the gauge (respecting the width you set).
        /// </summary>
        public double Scale { get; set; } = 1.0;
        /// <summary>
        /// Force the width of the gauge in pixels before scaling.
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// If to skip rendering this gauge.
        /// </summary>
        public bool Skip { get; set; } = false;
    }

    public class InternalGauge
    {
        public string? Source { get; set; } // absolute path to JSON file
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes a gauge and how to render it.
    /// </summary>
    public class Gauge : InternalGauge
    {
        /// <summary>
        /// The name of the gauge. Used for referencing it from a panel and for debugging.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The width of the gauge (in pixels).
        /// </summary>
        public required int Width { get; set; }
        /// <summary>
        /// The height of the gauge (in pixels).
        /// </summary>
        public required int Height { get; set; }
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
        public required List<Layer> Layers { get; set; }
        /// <summary>
        /// How to clip the layers of the gauge. Useful for gauges like an attitude indicator that translates outside of the gauge bounds.
        /// </summary>
        public ClipConfig? Clip { get; set; }
    }

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
        /// <default>SVG viewbox width or 100%</default>
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// The width of the SVG (in pixels).
        /// <default>SVG viewbox height or 100%</default>
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
    }

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
        /// A path to an image to render as this layer. PNG and SVG supported. If provided then `text` will be ignored.
        /// </summary>
        public string? Image { get; set; }
        /// <summary>
        /// The width of the layer (in pixels).
        /// </summary>
        /// <default>SVG viewbox width or PNG width or 100%</default>
        public double? Width { get; set; }
        /// <summary>
        /// The height of the layer (in pixels).
        /// </summary>
        /// <default>SVG viewbox height or PNG height or 100%</default>
        public double? Height { get; set; }
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
        /// Render useful debugging visuals such as bounding box.
        /// Note: If you subscribe to a SimVar in this layer and debugging is enabled it is sent to the server for extra logging.
        /// </summary>
        public bool Debug { get; set; } = false;
        /// <summary>
        /// If to skip rendering this layer.
        /// </summary>
        public bool? Skip { get; set; } = false;
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes what kind of text to render in the layer.
    /// </summary>
    public class TextDef
    {
        [JsonConverter(typeof(VarConfigConverter))]
        /// <summary>
        /// How to subscribe to a SimVar (and its unit) as the source of the text. eg. ["AIRSPEED INDICATED", "knots"]
        /// Note all vars are requested as floats so units like "position" -127..127 are mapped to -1..1.
        /// <type>[string, string]</type>
        /// </summary>
        public VarConfig? Var { get; set; }
        /// <summary>
        /// The default text to render when there is no SimVar value.
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
        /// </summary>
        /// <default>OS default ("Segoe UI" on Windows)<default>
        public string? FontFamily { get; set; }
        /// <summary>
        /// Path to a font file to use. Relative to the config JSON file.
        /// </summary>
        public string? Font { get; set; }
        [JsonConverter(typeof(ColorDefConverter))]
        /// <summary>
        /// The color of the text as a CSS-like value.
        /// eg. "rgb(255, 255, 255)" or "#FFF" or "white"
        /// </summary>
        /// <default>rgb(255, 255, 255)</default>
        public ColorDef? Color { get; set; }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how to transform a layer using SimVars.
    /// </summary>
    public class TransformDef
    {
        public RotateConfig? Rotate { get; set; }
        public TranslateConfig? TranslateX { get; set; }
        public TranslateConfig? TranslateY { get; set; }
        public PathConfig? Path { get; set; }
    }

    [GenerateMarkdownTable]
    public class CalibrationPoint
    {
        public required double Value { get; set; }
        public required double Degrees { get; set; }
    }

    public class VarConfig
    {
        /// <summary>
        /// The name of the SimVar straight from the data source eg. "AIRSPEED INDICATED".
        /// Full list: https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The unit of the SimVar straight from the data source. eg. "knot" or "degrees".
        /// Full list: https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variable_Units.htm
        /// </summary>
        public required string Unit { get; set; }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// How to transform a layer using a SimVar.
    /// </summary>
    public class TransformConfig
    {
        [JsonConverter(typeof(VarConfigConverter))]
        /// <summary>
        /// The SimVar and its unit to subscribe to. eg. ["AIRSPEED INDICATED", "knots"]
        /// Note all vars are requested as floats so units like "position" -127..127 are mapped to -1..1.
        /// <type>[string, string]</type>
        /// </summary>
        public required VarConfig Var { get; set; }
        /// <summary>
        /// The minimum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to.
        /// </summary>
        public double? From { get; set; }
        /// <summary>
        /// The maximum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to.
        /// </summary>
        public double? To { get; set; }
        /// <summary>
        /// The minimum possible value for the SimVar. eg. for airspeed it would be 0 for 0 knots
        /// </summary>
        public double? Min { get; set; }
        /// <summary>
        /// The maximum possible value for the SimVar.
        /// </summary>
        public double? Max { get; set; }
        /// <summary>
        /// If to invert the resulting rotation/translation.
        /// </summary>
        public bool Invert { get; set; } = false;
        /// <summary>
        /// How much to multiply the SimVar amount by. Useful to convert "feet per second" into "feet per minute".
        /// </summary>
        public double? Multiply { get; set; }
        /// <summary>
        /// How to "calibrate" raw SimVar values to specific angles because there is not a linear relationship.
        /// Some gauges are not linear so require calibration (such as the C172 ASI).
        /// </summary>
        public List<CalibrationPoint>? Calibration { get; set; }
        /// <summary>
        /// If to skip applying this transform.
        /// </summary>
        public bool? Skip { get; set; }
        /// <summary>
        /// Extra logging. Beware of console spam!
        /// </summary>
        public bool? Debug { get; set; }
        /// <summary>
        /// Force a SimVar value for debugging purposes.
        /// </summary>
        public double? Override { get; set; }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how a layer should rotate. Inherits from `TransformConfig`.
    /// </summary>
    public class RotateConfig : TransformConfig
    {
        /// <summary>
        /// If to allow the rotation to "wrap" around 360 degrees such as with an altimeter.
        /// </summary>
        public bool Wrap { get; set; } = false;
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how a layer should translate. Inherits from `TransformConfig`.
    /// </summary>
    public class TranslateConfig : TransformConfig { }

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
        /// <default>SVG viewbox width or 100%</default>
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// The width of the SVG (in pixels).
        /// <default>SVG viewbox height or 100%</default>
        /// </summary>
        public double? Height { get; set; }
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
        public FlexibleVector2 Position { get; set; } = new();
    }
}