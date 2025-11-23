using System.Diagnostics;
using System.Text.Json;

namespace OpenGaugeClient
{
    public class VarManager
    {
        public static VarManager Instance { get; } = new();

        public struct SimVarSample
        {
            public double PrevValue;
            public long PrevTime;

            public double LastValue;
            public long LastTime;
        }

        private readonly Dictionary<(string Name, string Unit), SimVarSample> simVarValues
            = new();

        public void StoreVar(string name, string unit, object? value)
        {
            var key = (name, unit);

            double? parsed = null;
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number)
                    parsed = je.GetDouble();
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
        }

        public object? GetSimVarValue(string name, string unit)
        {
            if (simVarValues.TryGetValue((name, unit), out var s))
                return s;
            else
                return null;
        }

        public object? GetInterpolatedSimVarValue(string name, string unit)
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