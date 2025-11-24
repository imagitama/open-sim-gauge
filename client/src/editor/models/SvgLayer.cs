using System.Text.Json.Serialization;

namespace OpenGaugeClient.Editor
{
    public sealed class ShadowUnion
    {
        public ShadowConfig? Config { get; init; }
        public bool IsTrue { get; init; }
        public bool IsFalse { get; init; }
        public bool IsMissingOrNull => !IsTrue && !IsFalse && Config is null;
        public ShadowConfig? ToConfig()
        {
            if (IsTrue)
                return new ShadowConfig();

            if (Config != null)
                return Config;

            return null;
        }

        public override string ToString()
        {
            return $"Shadow({(IsTrue ? "true" : IsFalse ? "false" : Config != null ? Config.ToString() : "null")})";
        }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes a shadow for a SVG layer.
    /// </summary>
    public class ShadowConfig
    {
        /// <summary>
        /// Shadow blur radius (stdDeviation in Gaussian blur).
        /// </summary>
        public double Size { get; set; } = 4;

        /// <summary>
        /// Horizontal offset of the shadow.
        /// </summary>
        public double OffsetX { get; set; } = 3;

        /// <summary>
        /// Vertical offset of the shadow.
        /// </summary>
        public double OffsetY { get; set; } = 3;

        public ShadowConfig() { }

        public ShadowConfig(double size, double offsetX, double offsetY)
        {
            Size = size;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }

        public override string ToString()
        {
            return $"ShadowConfig(size={Size},offsetX={OffsetX},offsetY={OffsetY})";
        }
    }

    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how to create an SVG.
    /// </summary>
    public class SvgLayer
    {
        /// <summary>
        /// The name of the layer which becomes a SVG image.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The operations to perform to create the SVG.
        /// </summary>
        public List<SvgOperation> Operations { get; set; } = new();
        [JsonConverter(typeof(ShadowUnionConverter))]
        /// <summary>
        /// If to render a shadow.
        /// <type>true | `ShadowConfig`</type>
        /// </summary>
        public ShadowUnion? Shadow { get; set; }

        public override string ToString()
        {
            return $"SvgLayer(" +
                   $"Name={Name}," +
                   $"Operations=\n{string.Join("\n", Operations.Select(l => $"  {l}"))}\n," +
                   $"Shadow={Shadow}" +
                ")";
        }

        public void Replace(SvgLayer newSvgLayer)
        {
            Name = newSvgLayer.Name;
            Operations = newSvgLayer.Operations;
        }
    }
}