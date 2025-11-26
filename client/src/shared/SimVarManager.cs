using System.Diagnostics;
using System.Text.Json;

namespace OpenGaugeClient
{
    public class SimVarManager
    {
        public static SimVarManager Instance { get; } = new();

        public struct SimVarSample
        {
            public double PrevValue;
            public long PrevTime;

            public double LastValue;
            public long LastTime;
        }

        private readonly Dictionary<(string Name, string Unit), SimVarSample> simVarValues
            = new();

        public void StoreSimVar(string name, string unit, object? value)
        {
            var key = (name, unit);

            if (value == null)
            {
                // assume we try and get it later and default to null
                return;
            }

            double? parsed = null;

            switch (value)
            {
                case JsonElement je when je.ValueKind == JsonValueKind.Number:
                    parsed = je.GetDouble();
                    break;

                case double d:
                    parsed = d;
                    break;

                case float f:
                    parsed = f;
                    break;

                case int i:
                    parsed = i;
                    break;

                case long l:
                    parsed = l;
                    break;
            }

            if (parsed is double newValue)
            {
                var now = Stopwatch.GetTimestamp();

                if (!simVarValues.TryGetValue(key, out var sample))
                {
                    sample = new SimVarSample
                    {
                        PrevValue = newValue,
                        LastValue = newValue,
                        PrevTime = now,
                        LastTime = now
                    };
                }
                else
                {
                    sample.PrevValue = sample.LastValue;
                    sample.PrevTime = sample.LastTime;

                    sample.LastValue = newValue;
                    sample.LastTime = now;
                }

                simVarValues[key] = sample;
            }

            if (parsed == null)
                throw new Exception($"Failed to parse {name} ({unit}) value >>{value}<< type {(value != null ? value.GetType() : "null")}");
        }

        public double? GetSimVarValue(string name, string unit)
        {
            if (simVarValues.TryGetValue((name, unit), out var s))
                return s.LastValue;
            else
                return null;
        }

        public double? GetBestSimVarValue(string name, string unit)
        {
            if (ConfigManager.Config.Interpolate != false)
                return GetInterpolatedSimVarValue(name, unit);

            return GetSimVarValue(name, unit);
        }

        public double? GetInterpolatedSimVarValue(string name, string unit)
        {
            if (!simVarValues.TryGetValue((name, unit), out var s))
                return null;

            long now = Stopwatch.GetTimestamp();

            long dt = s.LastTime - s.PrevTime;
            if (dt <= 0)
                return s.LastValue;

            // time between the two samples
            double alpha = (double)(now - s.LastTime) / dt;

            // avoid overshooting if frames are slow
            alpha = Math.Clamp(alpha, 0, 1);

            // interpolate
            return s.PrevValue + (s.LastValue - s.PrevValue) * alpha;
        }
    }
}