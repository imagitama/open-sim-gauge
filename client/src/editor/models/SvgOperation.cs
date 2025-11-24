using System.Text.Json.Serialization;

namespace OpenGaugeClient.Editor
{
    public enum SvgOperationType
    {
        Circle,
        Arc,
        GaugeTicks,
        GaugeTickLabels,
        Text,
        Square,
        Triangle
    }

    [GenerateMarkdownTable]
    [JsonConverter(typeof(SvgOperationConverter))]
    /// <summary>
    /// An object that describes an operation to perform in the SVG.
    /// Usually equates to a SVG element like a <circle> etc.
    /// </summary>
    public abstract class SvgOperation
    {
        /// <summary>
        /// The name of the operation. Helpful for identifying in a long list of operations.
        /// </summary>
        public string? Name { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        /// <summary>
        /// The type of operation.
        /// </summary>
        public abstract SvgOperationType Type { get; }
        [JsonConverter(typeof(FlexibleDimensionConverter))]
        /// <summary>
        /// The width of the operation. SVG units or percent.
        /// <default>SVG width</default>
        /// </summary>
        public FlexibleDimension? Width { get; set; }
        [JsonConverter(typeof(FlexibleDimensionConverter))]
        /// <summary>
        /// The height of the operation. SVG units or percent.
        /// <default>SVG height</default>
        /// </summary>
        public FlexibleDimension? Height { get; set; }
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The position of the operation inside the SVG.
        /// </summary>
        public FlexibleVector2 Position { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };
        [JsonConverter(typeof(FlexibleVector2Converter))]
        /// <summary>
        /// The origin of the operation. Used for positioning.
        /// </summary>
        public FlexibleVector2 Origin { get; set; } = new()
        {
            X = "50%",
            Y = "50%"
        };
        /// <summary>
        /// A rotation in degrees relative to 0 degrees "north" to apply to the operation.
        /// </summary>
        public double? Rotate { get; set; }
        /// <summary>
        /// If to skip this operation.
        /// </summary>
        public bool Skip { get; set; } = false;

        public override string ToString()
        {
            return $"SvgOperation(" +
                   $"Type={Type}," +
                   $"Name={Name}, " +
                   $"Type={Type}, " +
                   $"Width={Width}, " +
                   $"Height={Height}, " +
                   $"Position={Position}, " +
                   $"Origin={Origin}, " +
                   $"Rotate={Rotate}, " +
                   $"Skip={Skip}" +
            ")";
        }
    }

    public class TextSvgOperation : SvgOperation
    {
        public override SvgOperationType Type => SvgOperationType.Text;
        /// <summary>
        /// The text to render.
        /// </summary>
        public required string Text { get; set; }
        /// <summary>
        /// The size of the text.
        /// </summary>
        public double Size { get; set; } = 24;
        /// <summary>
        /// The font family to use.
        /// If your gauge loads a custom font file you can use its family name.
        /// Otherwise use system-friendly fonts like Arial.
        /// </summary>
        public string Font { get; set; } = "Arial";
        /// <summary>
        /// The color of the text. Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string Fill { get; set; } = "rgb(0,0,0)";
    }

    public class GaugeTicksSvgOperation : SvgOperation
    {
        public override SvgOperationType Type => SvgOperationType.GaugeTicks;
        public required double Radius { get; set; }
        /// <summary>
        /// At what degrees the arc starts. Relative to 0 degrees "up". Can be negative.
        /// </summary>
        public required double DegreesStart { get; set; }
        /// <summary>
        /// At what degrees the arc ends. Relative to 0 degrees "up". Can be negative.
        /// </summary>
        public required double DegreesEnd { get; set; }
        /// <summary>
        /// The gap between ticks.
        /// </summary>
        public required double DegreesGap { get; set; }
        /// <summary>
        /// The length of the ticks.
        /// </summary>
        public double TickLength { get; set; } = 10;
        /// <summary>
        /// The width of the ticks.
        /// </summary>
        public double TickWidth { get; set; } = 5;
        /// <summary>
        /// The color of the ticks. Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string TickFill { get; set; } = "rgb(255,255,255)";
    }

    public class SquareSvgOperation : SvgOperation
    {
        public override SvgOperationType Type => SvgOperationType.Square;
        /// <summary>
        /// The color of the shape. Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string Fill { get; set; } = "rgb(255,255,255)";
        /// <summary>
        /// The color of the stroke (border). Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string? StrokeFill { get; set; }
        /// <summary>
        /// The width of the stroke (border). Leave null to disable.
        /// </summary>
        public double? StrokeWidth { get; set; }
        /// <summary>
        /// A border radius to apply to the square.
        /// </summary>
        public double? Round { get; set; }
    }

    public class TriangleSvgOperation : SvgOperation
    {
        public override SvgOperationType Type => SvgOperationType.Triangle;
        /// <summary>
        /// The color of the shape. Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string Fill { get; set; } = "rgb(255,255,255)";
        /// <summary>
        /// The color of the stroke (border). Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string? StrokeFill { get; set; }
        /// <summary>
        /// The width of the stroke (border). Leave null to disable.
        /// </summary>
        public double? StrokeWidth { get; set; }
        /// <summary>
        /// A border radius to apply to the triangle.
        /// </summary>
        public double? Round { get; set; }
    }

    public class CircleSvgOperation : SvgOperation
    {
        public override SvgOperationType Type => SvgOperationType.Circle;
        /// <summary>
        /// The color of the shape. Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string Fill { get; set; } = "rgb(255,255,255)";
        /// <summary>
        /// The color of the stroke (border). Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string? StrokeFill { get; set; }
        /// <summary>
        /// The width of the stroke (border). Leave null to disable.
        /// </summary>
        public double? StrokeWidth { get; set; }
        public required double Radius { get; set; }
    }

    public class GaugeTickLabelsSvgOperation : SvgOperation
    {
        public override SvgOperationType Type => SvgOperationType.GaugeTickLabels;
        /// <summary>
        /// The radius of the arc.
        /// </summary>
        public required double Radius { get; set; }
        /// <summary>
        /// At what degrees the arc starts. Relative to 0 degrees "up". Can be negative.
        /// </summary>
        public required double DegreesStart { get; set; }
        /// <summary>
        /// At what degrees the arc ends. Relative to 0 degrees "up". Can be negative.
        /// </summary>
        public required double DegreesEnd { get; set; }
        /// <summary>
        /// The gap between each label.
        /// </summary>
        public required double DegreesGap { get; set; }
        /// <summary>
        /// The labels to use. For a speedometer it would be something like ['0', '10', '20', '30'].
        /// </summary>
        public required IList<string> Labels { get; set; }
        /// <summary>
        /// The size of the labels.
        /// </summary>
        public double LabelSize { get; set; } = 24;
        /// <summary>
        /// The color of the labels. Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string LabelFill { get; set; } = "rgb(255,255,255)";
        /// <summary>
        /// The font family to use.
        /// If your gauge loads a custom font file you can use its family name.
        /// Otherwise use system-friendly fonts like Arial.
        /// </summary>
        public string LabelFont { get; set; } = "Arial";
    }

    public class ArcSvgOperation : SvgOperation
    {
        public override SvgOperationType Type => SvgOperationType.Arc;
        /// <summary>
        /// The radius of the arc.
        /// </summary>
        public required double Radius { get; set; }
        /// <summary>
        /// At what degrees the arc starts. Relative to 0 degrees "up". Can be negative.
        /// </summary>
        public required double DegreesStart { get; set; }
        /// <summary>
        /// At what degrees the arc ends. Relative to 0 degrees "up". Can be negative.
        /// </summary>
        public required double DegreesEnd { get; set; }
        /// <summary>
        /// The thickness of the arc, but starting from the radius of the arc. Useful for gauge markings.
        /// </summary>
        public required double InnerThickness { get; set; }
        /// <summary>
        /// The color of the arc. Use a SVG-friendly color like rgb(0,0,0) or hex.
        /// </summary>
        public string Fill { get; set; } = "rgb(255,0,0)";
    }

    public static class SvgOperationFactory
    {
        public static SvgOperation Create(SvgOperationType type)
        {
            return type switch
            {
                SvgOperationType.Circle => new CircleSvgOperation()
                {
                    Radius = 100
                },
                SvgOperationType.Arc => new ArcSvgOperation()
                {
                    Radius = 100,
                    DegreesStart = -90,
                    DegreesEnd = 45,
                    InnerThickness = 10
                },
                SvgOperationType.GaugeTicks => new GaugeTicksSvgOperation()
                {
                    Radius = 100,
                    DegreesStart = -90,
                    DegreesEnd = 45,
                    DegreesGap = 10
                },
                SvgOperationType.GaugeTickLabels => new GaugeTickLabelsSvgOperation()
                {
                    Radius = 100,
                    DegreesStart = -90,
                    DegreesEnd = 45,
                    DegreesGap = 10,
                    Labels = ["0", "10", "20"]
                },
                SvgOperationType.Text => new TextSvgOperation()
                {
                    Text = "abc"
                },
                SvgOperationType.Square => new SquareSvgOperation(),
                SvgOperationType.Triangle => new TriangleSvgOperation(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

}