namespace OpenGaugeClient
{
    public static class GaugeHelper
    {
        public static Gauge GetGaugeByName(string name)
        {
            var gauge = ConfigManager.Config!.Gauges.Find(gauge => gauge.Name == name);

            if (gauge == null)
                throw new Exception($"Failed to get gauge by name: {name}");

            return gauge;
        }

        public static int GetIndexByName(string name)
        {
            var gaugeIndex = ConfigManager.Config!.Gauges.FindIndex(gauge => gauge.Name == name);

            if (gaugeIndex == -1)
                throw new Exception($"Failed to get gauge index by name: {name}");

            return gaugeIndex;
        }

        public static async Task<Gauge> GetGaugeByPath(string path)
        {
            var absolutePath = PathHelper.GetFilePath(path, forceToGitRoot: true);

            var gauge = await JsonHelper.LoadTypedJson<Gauge>(absolutePath);

            if (gauge == null)
                throw new Exception($"Failed to get gauge by path: {path}");

            gauge.Source = absolutePath;

            return gauge;
        }

        public static async Task SaveGaugeToFile(Gauge gaugeToSave)
        {
            if (gaugeToSave.Source == null)
                throw new Exception("Cannot save without a source");

            var jsonPath = gaugeToSave.Source;

            Console.WriteLine($"[GaugeHelper] Save gauge to file gauge={gaugeToSave} path={jsonPath}");

            await JsonHelper.SaveJson(gaugeToSave, jsonPath);
        }

        public static List<Gauge> FindGaugesReferencedByPathInAllPanels()
        {
            var gauges = ConfigManager.Config.Panels.SelectMany(panel => panel.Gauges.Where(g => g.Gauge?.Source != null).Select(g => g.Gauge)).ToList();
            return gauges as List<Gauge>;
        }

        public static double MapSimVarValueToOffset(TransformConfig config, double? varValue)
        {
            if (varValue == null)
                return 0;

            double value = (double)varValue;

            if (config.Multiply != null)
                value *= (double)config.Multiply;

            if (config.Invert == true)
                value *= -1;

            var varConfig = config.Var;
            string unit = varConfig.Unit;

            var calibration = config.Calibration;
            if (calibration != null && calibration.Count > 0)
            {
                // clamp
                if (value <= calibration[0].Value)
                    return calibration[0].Degrees;
                if (value >= calibration[^1].Value)
                    return calibration[^1].Degrees;

                // find nearest two calibration points and interpolate
                for (int i = 0; i < calibration.Count - 1; i++)
                {
                    var a = calibration[i];
                    var b = calibration[i + 1];
                    if (value >= a.Value && value <= b.Value)
                    {
                        double t = (value - a.Value) / (b.Value - a.Value);
                        double angle = a.Degrees + t * (b.Degrees - a.Degrees);

                        return angle;
                    }
                }
            }

            // if user doesnt want any clamping or anything
            if (config.Min == null && config.Max == null && config.From == null && config.To == null)
                return value;

            if (unit == "radians")
            {
                // normalize wrap-around radians into a centered -π..+π range
                if (value > Math.PI)
                    value -= 2 * Math.PI;
            }

            // TODO: at minimum document this OR just remove completely and beg user to do it
            double defaultMin, defaultMax;
            switch (unit)
            {
                case "feet": defaultMin = 0; defaultMax = 10000; break;
                case "knots": defaultMin = 0; defaultMax = 200; break;
                case "rpm": defaultMin = 0; defaultMax = 3000; break;
                case "fpm": defaultMin = -2000; defaultMax = 2000; break;
                case "position": defaultMin = -127; defaultMax = 127; break;
                case "radians":
                    defaultMin = -Math.PI;
                    defaultMax = Math.PI;
                    break;
                default:
                    defaultMin = 0;
                    defaultMax = 1;
                    break;
            }

            double inputMin = config.Min ?? defaultMin;
            double inputMax = config.Max ?? defaultMax;
            double outputFrom = config.From ?? 0;
            double outputTo = config.To ?? 1;

            double range = inputMax - inputMin;
            if (range <= 0)
                return outputFrom;

            double normalized;
            if (config is RotateConfig rotate && rotate.Wrap)
            {
                // otherwise 0
                if (value == inputMax)
                {
                    normalized = 1;
                }
                else
                {
                    // how many times it has exceeded the max
                    double revolutionsCount = (value - inputMin) / range;

                    // map it back to something we can render
                    normalized = revolutionsCount - Math.Floor(revolutionsCount);
                }
            }
            else
            {
                normalized = (value - inputMin) / range;
                normalized = Math.Clamp(normalized, 0, 1);
            }

            var finalValue = outputFrom + (outputTo - outputFrom) * normalized;

            return finalValue;
        }
    }
}