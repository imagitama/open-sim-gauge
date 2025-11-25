using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
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
        /// If to always render this panel.
        /// </summary>
        public bool? Force { get; set; } = false;
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
}