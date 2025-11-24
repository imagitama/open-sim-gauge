using System.Text.Json.Serialization;

namespace OpenGaugeClient.Editor
{
    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how to create an SVG.
    /// </summary>
    public class SvgCreator
    {
        /// <summary>
        /// The individual SVG images that you would use as layers in a gauge.
        /// </summary>
        public required List<SvgLayer> Layers { get; set; }
        /// <summary>
        /// The (viewbox) width of all SVGs.
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// The (viewbox) height of all SVGs.
        /// </summary>
        public double Height { get; set; }

        public override string ToString()
        {
            return $"SvgCreator(" +
                   $"Width={Width}," +
                   $"Height={Height}," +
                   $"Layers=\n{string.Join("\n", Layers.Select(l => $"  {l}"))}\n," +
                ")";
        }

        public void Replace(SvgCreator newSvgCreator)
        {
            Width = newSvgCreator.Width;
            Height = newSvgCreator.Height;
            Layers = newSvgCreator.Layers;
        }

        [JsonIgnore]
        public string? Source { get; set; }
    }
}