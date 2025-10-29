using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    class ConfigManager
    {
        public static Config? Config { get; private set; }

        public static async Task<Config> LoadConfig()
        {
            return await LoadJson<Config>("config.json");
        }

        public static async Task<T> LoadJson<T>(string filePath)
        {
            string absoluteFilePath = PathHelper.GetFilePath(filePath);

            Console.WriteLine($"[ConfigManager] Load JSON: {absoluteFilePath}");

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

                var options = new JsonSerializerOptions { 
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

    public class Config
    {
        // public string? Name { get; set; } // name of the client - for sending remote configs
        public bool? Debug { get; set; }
        public int? Fps { get; set; }
        public ServerConfig? Server { get; set; }
        public required List<Panel> Panels { get; set; }
        public List<Gauge> Gauges { get; set; } = new();
    }
    
    public class ServerConfig
    {
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        // public bool? ImportConfig { get; set; } // if to tell server to send it any configs that are for me
    }

    public class Panel
    {
        public required string Name { get; set; }
        public required List<GaugeRef> Gauges { get; set; }
        public bool? Skip { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool? Fullscreen { get; set; }
        public int[]? Position { get; set; }
        public int? Screen { get; set; }
        [JsonConverter(typeof(ColorDefConverter))]
        public ColorDef? Background { get; set; }
        public bool? Debug { get; set; }
        // public string[]? Aircraft { get; set; } // aircraft names to decide which panel to use
        // public string Client { get; set; } // name of client to load it for
    }

    public class GaugeRef
    {
        public string? Name { get; set; } // optional - use path
        public required int[] Position { get; set; }
        public double? Scale { get; set; }
        public bool? Skip { get; set; }
        public string? Path { get; set; } // path to a JSON file that contains a gauge definition
    }

    public class Gauge
    {
        // public bool? Clip { get; set; }
        public required string Name { get; set; }
        public required int Width { get; set; }
        public required int Height { get; set; }
        public int[]? Origin { get; set; }
        public required List<Layer> Layers { get; set; }
        public ClipConfig? Clip { get; set; }
        // internal
        public string? Source { get; set; } // absolute path to JSON file
    }

    public class ClipConfig
    {
        public required string Image { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int[]? Origin { get; set; }
        public int[]? Position { get; set; }
    }

    public class Layer
    {
        /// <summary>
        /// The name of the layer. Only used for debugging and readability purposes.
        /// </summary>
        public string? Name { get; set; }
        public TextDef? Text { get; set; }
        public string? Image { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int[]? Origin { get; set; }
        public int[]? Position { get; set; }
        public TransformDef? Transform { get; set; }
        /// <summary>
        /// How many degrees to rotate the layer initially.
        /// </summary>
        public double? Rotate { get; set; }
        /// <summary>
        /// How much to translate the layer on the X axis initially.
        /// </summary>
        public double? TranslateX { get; set; }
        /// <summary>
        /// How much to translate the layer on the Y axis initially.
        /// </summary>
        public double? TranslateY { get; set; }
        public bool? Debug { get; set; }
        public bool? Skip { get; set; }
    }

    public class TextDef
    {
        [JsonConverter(typeof(VarConfigConverter))]
        public VarConfig? Var { get; set; }
        public string? Default { get; set; }
        public int? FontSize { get; set; }
        public string? FontFamily { get; set; }
        [JsonConverter(typeof(ColorDefConverter))]
        public ColorDef? Color { get; set; }
    }

    public class TransformDef
    {
        public RotateConfig? Rotate { get; set; }
        // public ScaleDef? Scale { get; set; }
        public TranslateConfig? TranslateX { get; set; }
        public TranslateConfig? TranslateY { get; set; }
        public PathConfig? Path { get; set; }
    }

    public class CalibrationPoint
    {
        public required double Value { get; set; }
        public required double Degrees { get; set; }
    }

    public class VarConfigOptions
    {
        /// <summary>
        /// Some gauges are not linear so require calibration (such as the C172 ASI).
        /// </summary>
        public List<CalibrationPoint>? Calibration { get; set; }
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
        public VarConfigOptions? Options { get; set; }
    }

    public class TransformConfig
    {
        [JsonConverter(typeof(VarConfigConverter))]
        public required VarConfig Var { get; set; }
        public double? From { get; set; }
        public double? To { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool? Wrap { get; set; }
        public bool? Invert { get; set; }
        /// <summary>
        /// How much to multiply the SimVar amount by. Useful to convert "feet per second" into "feet per minute".
        /// </summary>
        public double? Multiply { get; set; }
        public bool? Skip { get; set; }
        public bool? Debug { get; set; }
        /// <summary>
        /// Force a SimVar value for debugging purposes.
        /// </summary>
        public double? Override { get; set; }
    }

    public class RotateConfig : TransformConfig { }
    public class TranslateConfig : TransformConfig { }

    public class PathConfig : TransformConfig
    {
        /// <summary>
        /// The SVG to use. It must contain a single <path> used to determine the clipping area.
        /// </summary>
        public required string Image { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int[]? Origin { get; set; }
        public int[]? Position { get; set; }
    }
}