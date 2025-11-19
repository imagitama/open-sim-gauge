namespace OpenGaugeClient
{
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
        public override string ToString()
        {
            return $"VarConfig(Name={Name ?? "null"},Unit={Unit ?? "null"})";
        }
    }
}