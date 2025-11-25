using Microsoft.Extensions.FileSystemGlobbing;

namespace OpenGaugeClient
{
    public static class VehicleUtils
    {
        public static bool GetIsVehicle(List<string> vehiclePatterns, string actualVehicleName)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

            foreach (var pattern in vehiclePatterns)
            {
                if (pattern.StartsWith("!"))
                    matcher.AddExclude(pattern.Substring(1));
                else
                    matcher.AddInclude(pattern);
            }

            var result = matcher.Match([actualVehicleName]);

            return result.HasMatches;
        }
    }
}