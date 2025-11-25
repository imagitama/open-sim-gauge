namespace OpenGaugeClient
{
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
}