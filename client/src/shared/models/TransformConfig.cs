using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    [GenerateMarkdownTable]
    /// <summary>
    /// An object that describes how to "map" a Var value into a specific degrees.
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
    /// How to transform a layer using a Var.
    /// </summary>
    public class TransformConfig
    {
        [JsonConverter(typeof(SimVarConfigConverter))]
        /// <summary>
        /// The var and its unit to subscribe to. eg. ["AIRSPEED INDICATED", "knots"]
        /// SimConnect note: all vars are requested as floats so units like "position" -127..127 are mapped to -1..1.
        /// <type>[string, string]</type>
        /// </summary>
        public required SimVarConfig Var { get; set; }
        /// <summary>
        /// The minimum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to.
        /// </summary>
        public double? From { get; set; }
        /// <summary>
        /// The maximum to translate/rotate. If the value is 50% the from->to then it will render at 50% from->to.
        /// </summary>
        public double? To { get; set; }
        /// <summary>
        /// The minimum possible value for the var. eg. for airspeed it would be 0 for 0 knots
        /// </summary>
        public double? Min { get; set; }
        /// <summary>
        /// The maximum possible value for the var.
        /// </summary>
        public double? Max { get; set; }
        /// <summary>
        /// If to invert the resulting rotation/translation.
        /// </summary>
        public bool? Invert { get; set; }
        /// <summary>
        /// How much to multiply the value amount by. Useful to convert "feet per second" into "feet per minute".
        /// </summary>
        public double? Multiply { get; set; }
        /// <summary>
        /// How to "calibrate" raw values to specific angles because there is not a linear relationship.
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
                $"Debug={Debug},";
        }
    }
}