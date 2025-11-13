using System.Text.RegularExpressions;

namespace OpenGaugeClient
{
    public static class Utils
    {
        // TODO: move to a VehicleHelper class or something
        public static bool GetIsVehicle(string vehicleName, string actualVehicleName)
        {
            string pattern = "^" + Regex.Escape(vehicleName).Replace("\\*", ".*") + "$";
            bool matches = Regex.IsMatch(actualVehicleName, pattern, RegexOptions.IgnoreCase);
            return matches;
        }
    }
}