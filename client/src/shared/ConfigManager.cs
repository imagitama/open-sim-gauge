using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Layout;

namespace OpenGaugeClient
{
    public static class ConfigManager
    {
        private static Config? _config;
        public static Config Config => _config ?? throw new InvalidOperationException("LoadConfig must be called first");

        public static async Task SavePanel(int panelIndex, Panel panelToSave)
        {
            Config.Panels[panelIndex] = panelToSave;

            await SaveConfig();
        }

        public static async Task AddPanel(Panel panelToAdd)
        {
            Config.Panels.Add(panelToAdd);

            await SaveConfig();
        }

        public static async Task DeletePanel(int panelIndex)
        {
            Config.Panels.RemoveAt(panelIndex);

            await SaveConfig();
        }

        public static async Task SaveGauge(int gaugeIndex, Gauge gaugeToSave)
        {
            Console.WriteLine($"[ConfigManager] Save gauge index={gaugeIndex} gauge={gaugeToSave}");

            Config!.Gauges[gaugeIndex] = gaugeToSave;

            await SaveConfig();
        }

        public static async Task AddGauge(Gauge gaugeToAdd)
        {
            Config.Gauges.Add(gaugeToAdd);

            await SaveConfig();
        }

        public static async Task DeleteGauge(int panelIndex)
        {
            Config.Gauges.RemoveAt(panelIndex);

            await SaveConfig();
        }

        public static async Task<Config> LoadConfig(string? overridePath = null)
        {
            var configPath = overridePath ?? PathHelper.GetFilePath("client.json", forceToGitRoot: false);

            var newConfig = await LoadTypedJson<Config>(configPath);

            if (_config?.Debug == true || newConfig.Debug)
                Console.WriteLine($"[ConfigManager] Loaded config from {configPath}");

            var _gaugeCache = new GaugeCache();

            foreach (var panel in newConfig.Panels)
            {
                foreach (var gaugeRef in panel.Gauges)
                {
                    Gauge? gauge;

                    if (gaugeRef.Path != null)
                    {
                        gauge = await _gaugeCache.Load(gaugeRef.Path);
                    }
                    else
                    {
                        gauge = newConfig.Gauges.Find(g => g.Name == gaugeRef.Name);
                    }

                    if (gauge == null)
                        Console.WriteLine($"Panel '{panel.Name}' has invalid gauge '{gaugeRef.Name}' or path '{gaugeRef.Path}'");

                    gaugeRef.Gauge = gauge;
                }
            }

            for (var i = 0; i < newConfig.Gauges.Count; i++)
            {
                var gauge = newConfig.Gauges[i];

                if (gauge.Path != null)
                {
                    var path = gauge.Path;

                    var newGauge = await _gaugeCache.Load(path);

                    gauge.Replace(newGauge);

                    newGauge.Source = path;

                    newConfig.Gauges[i] = newGauge;
                }
            }

            _config = newConfig;

            return newConfig;
        }

        public static async Task SaveConfig()
        {
            await SaveJson(Config, "client.json", false);
        }

        public static async Task SaveJson(object content, string relativePath, bool forceToGitRoot = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            string newJson = JsonSerializer.Serialize(content, options);

            string absoluteFilePath = PathHelper.GetFilePath(relativePath, forceToGitRoot);

            if (Config.Debug)
                Console.WriteLine($"Save config: {absoluteFilePath}");

            Directory.CreateDirectory(Path.GetDirectoryName(absoluteFilePath)!);
            await File.WriteAllTextAsync(absoluteFilePath, newJson);
        }

        private static string ExtractPropertyName(string message)
        {
            int start = message.IndexOf('\'');
            int end = message.IndexOf('\'', start + 1);

            if (start >= 0 && end > start)
                return message.Substring(start + 1, end - start - 1);

            return "unknown";
        }

        private static Type? ResolveTypeAtJsonPath(Type rootType, string fullPath)
        {
            string path = fullPath.StartsWith("$.") ? fullPath[2..] : fullPath;

            int lastDot = path.LastIndexOf('.');
            if (lastDot > 0)
                path = path.Substring(0, lastDot);

            Type currentType = rootType;

            if (string.IsNullOrWhiteSpace(path))
                return currentType;

            var segments = path.Split('.');

            foreach (var segment in segments)
            {
                string propName = segment;
                int bracket = segment.IndexOf('[');
                if (bracket >= 0)
                    propName = segment.Substring(0, bracket);

                var prop = currentType.GetProperty(propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null)
                    return null;

                Type propType = prop.PropertyType;

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType)
                    && propType != typeof(string))
                {
                    if (propType.IsArray)
                        currentType = propType.GetElementType()!;
                    else if (propType.IsGenericType)
                        currentType = propType.GetGenericArguments()[0];
                    else
                        return null;
                }
                else
                {
                    currentType = propType;
                }
            }

            return currentType;
        }

        private static bool IsUnknownPropertyError(string msg)
        {
            return msg.Contains("could not be mapped to any .NET member");
        }

        public static async Task<T> LoadTypedJson<T>(string filePath, bool forceToGitRoot = false)
        {
            string absoluteFilePath = PathHelper.GetFilePath(filePath, forceToGitRoot);

            if (!File.Exists(absoluteFilePath))
                throw new Exception($"JSON file not found: {absoluteFilePath}");

            string json = await File.ReadAllTextAsync(absoluteFilePath);

            try
            {
                var reader = new Utf8JsonReader(
                    Encoding.UTF8.GetBytes(json),
                    new JsonReaderOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    }
                );

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };

                var result = JsonSerializer.Deserialize<T>(ref reader, options);
                return result!;
            }
            catch (JsonException ex)
            {
                string unknown = ExtractPropertyName(ex.Message);
                string path = ex.Path ?? "";
                path = path.StartsWith("$.") ? path[2..] : path;

                if (IsUnknownPropertyError(ex.Message))
                {
                    var targetType = ResolveTypeAtJsonPath(typeof(T), path);

                    string available = targetType != null
                        ? string.Join(", ", targetType.GetProperties().Select(p => p.Name))
                        : "unknown";

                    Console.WriteLine(
                        $"Failed to load JSON file {absoluteFilePath}:\n" +
                        $"JSON property '{unknown}' at {path} is not recognized.\n" +
                        $"Available properties: {available}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"JSON property at {path} has unexpected value"
                    );
                }

                throw;
            }
        }
    }

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

    [GenerateMarkdownTable]
    /// <summary>
    /// Configure the server IP and port.
    /// </summary>
    public class ServerConfig
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 1234;
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes a panel.
    /// </summary>
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
        /// The index of the screen you want to render this panel on (starting at 0 which is usually your main one).
        /// </summary>
        public int? Screen { get; set; } = 0;
        /// <summary>
        /// The width of the panel in pixels.
        /// Optional if you use fullscreen.
        /// </summary>
        public double? Width { get; set; }
        /// <summary>
        /// The width of the panel in pixels.
        /// Optional if you use fullscreen.
        /// </summary>
        public double? Height { get; set; }
        /// <summary>
        /// If to have the panel fill the screen.
        /// </summary>
        public bool Fullscreen { get; set; } = false;
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The position of the panel inside the screen (where 0,0 is the top-left of the specific screen). 
        /// X or Y can be a pixel value or a string which is a percent of the screen.
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
        public bool? Transparent { get; set; }
        /// <summary>
        /// If to render this panel above all other desktop windows and apps.
        /// </summary>
        public bool? OnTop { get; set; }
        /// <summary>
        /// Renders a grid with the provided cell size.
        /// </summary>
        public double? Grid { get; set; }
        /// <summary>
        /// If to clip all gauges.
        /// </summary>
        public bool? Clip { get; set; } = true;
        /// <summary>
        /// If to skip rendering this panel.
        /// </summary>
        public bool? Skip { get; set; } = false;
        /// <summary>
        /// Extra console logging for this panel.
        /// </summary>
        public bool? Debug { get; set; } = false;
        public Panel Clone()
        {
            return new Panel
            {
                Name = Name,
                Vehicle = Vehicle,
                Gauges = [.. Gauges.Select(g => g.Clone())],
                Screen = Screen,
                Width = Width,
                Height = Height,
                Fullscreen = Fullscreen,
                Position = new FlexibleVector2 { X = Position.X, Y = Position.Y },
                Origin = new FlexibleVector2 { X = Origin.X, Y = Origin.Y },
                Background = Background != null
                    ? new ColorDef(Background.R, Background.G, Background.B)
                    : null,
                OnTop = OnTop,
                Grid = Grid,
                Clip = Clip,
                Transparent = Transparent,
                Skip = Skip,
                Debug = Debug
            };
        }
        public override string ToString()
        {
            return $"Panel(" +
                $"Name={Name}," +
                $"Vehicle={Vehicle}," +
                $"Gauges=\n{string.Join("\n", Gauges.Select(l => $"  {l}"))}\n," +
                $"Skip={Skip}," +
                $"Screen={Screen}," +
                $"Width={Width}," +
                $"Height={Height}," +
                $"Fullscreen={Fullscreen}," +
                $"Position={Position}," +
                $"Origin={Origin}," +
                $"Background={Background}," +
                $"OnTop={OnTop}," +
                $"Transparent={Transparent}," +
                $"Debug={Debug}" +
                ")";
        }
    }

    public class InternalGaugeRef
    {
        [JsonIgnore]
        public Gauge? Gauge;
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes a reference to a gauge to render inside a panel.
    /// </summary>
    public class GaugeRef : InternalGaugeRef
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
    }

    public class InternalGauge
    {
        [JsonIgnore]
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

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how to transform a layer using vars.
    /// </summary>
    public class TransformDef
    {
        public RotateConfig? Rotate { get; set; }
        public TranslateConfig? TranslateX { get; set; }
        public TranslateConfig? TranslateY { get; set; }
        public PathConfig? Path { get; set; }
        public override string ToString()
        {
            return $"TransformDef(" +
                $"Rotate={Rotate}," +
                $"TranslateX={TranslateX}," +
                $"TranslateY={TranslateY}," +
                $"Path={Path}" +
            ")";
        }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how to "map" a SimVar value into a specific degrees.
    /// Useful for non-linear gauges like a C172 airspeed indicator.
    /// When rendering the actual degrees is interpolated between calibration points.
    /// </summary>
    public class CalibrationPoint
    {
        public required double Value { get; set; }
        public required double Degrees { get; set; }
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
        public override string ToString()
        {
            return $"Var={Var}," +
                $"From={From}," +
                $"To={To}," +
                $"Min={Min}," +
                $"Max={Max}," +
                $"Invert={Invert}," +
                $"Multiply={Multiply}," +
                $"Calibration={Calibration}," +
                $"Skip={Skip}," +
                $"Debug={Debug}," +
                $"Override={Override},";
        }
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
        public override string ToString()
        {
            return $"RotateConfig(" +
                base.ToString() +
                $"Wrap={Wrap}" +
                ")";
        }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how a layer should translate. Inherits from `TransformConfig`.
    /// </summary>
    public class TranslateConfig : TransformConfig
    {
        public override string ToString()
        {
            return $"TranslateConfig(" +
                base.ToString() +
                ")";
        }
    }

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