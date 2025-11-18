namespace OpenGaugeClient
{
    public class FlexibleDimension
    {
        private object _value;

        private double? _lastTotal;
        private double? _cachedResolved;

        public FlexibleDimension(object value)
        {
            _value = Normalize(value);
        }

        public object Value
        {
            get => _value;
            set
            {
                _value = Normalize(value);
                _cachedResolved = null;
            }
        }

        private static object Normalize(object? input)
        {
            if (input is null) return null!;

            switch (input)
            {
                case double d: return d;
                case float f: return (double)f;
                case int i: return (double)i;
                case long l: return (double)l;

                case string s:
                    if (double.TryParse(s, out var numOnly))
                        return numOnly;

                    return s;
            }

            return input;
        }

        private static double ResolveValue(object v, double total)
        {
            switch (v)
            {
                case string s when s.EndsWith("%") &&
                                   double.TryParse(s.TrimEnd('%'), out var p):
                    double pct = p / 100.0;
                    return pct >= 0 ? pct * total : total + (pct * total);

                case double d:
                    return d >= 0 ? d : total + d;

                case int i:
                    return i >= 0 ? i : total + i;

                default:
                    return 0;
            }
        }

        public double Resolve(double total, bool useCache = false)
        {
            bool cacheValid = useCache &&
                              _cachedResolved != null &&
                              _lastTotal == total;

            if (cacheValid)
                return _cachedResolved!.Value;

            double resolved = ResolveValue(_value, total);

            if (useCache)
            {
                _lastTotal = total;
                _cachedResolved = resolved;
            }

            return resolved;
        }

        public override string ToString() => $"{Value}";
    }
}
