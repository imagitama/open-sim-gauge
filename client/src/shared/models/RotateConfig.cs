namespace OpenGaugeClient
{
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
}