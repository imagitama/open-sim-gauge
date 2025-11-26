namespace OpenGaugeClient
{
    public class SimVarConfig
    {
        /// <summary>
        /// The name of the var straight from the data source eg. "AIRSPEED INDICATED".
        /// Full list of SimConnect vars: https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The unit of the var straight from the data source. eg. "knot" or "degrees" or "kph".
        /// Full list of SimConnect units: https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variable_Units.htm
        /// </summary>
        public required string Unit { get; set; }
        /// <summary>
        /// Force a value for debugging purposes.
        /// </summary>
        public double? Override { get; set; }
        public override string ToString()
        {
            return $"SimVarConfig(Name={Name},Unit={Unit ?? "null"},Override={(Override != null ? Override : "null")})";
        }
    }
}