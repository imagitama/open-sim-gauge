namespace OpenGaugeClient
{
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
}