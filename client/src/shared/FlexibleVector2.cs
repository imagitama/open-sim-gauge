namespace OpenGaugeClient
{
    public class FlexibleVector2
    {
        private object _x = 0;
        private object _y = 0;

        public object X
        {
            get => _x;
            set
            {
                _x = TryConvertToDouble(value);
                _cachedX = null;
            }
        }

        public object Y
        {
            get => _y;
            set
            {
                _y = TryConvertToDouble(value);
                _cachedY = null;
            }
        }

        private static object TryConvertToDouble(object? value)
        {
            if (value is null)
                return null!;

            switch (value)
            {
                case double d:
                    return d;
                case float f:
                    return (double)f;
                case int i:
                    return (double)i;
                case long l:
                    return (double)l;
                case string s when double.TryParse(s, out var parsed):
                    return parsed;
                default:
                    return value;
            }
        }

        private double? _lastTotalWidth;
        private double? _lastTotalHeight;
        private double? _cachedX;
        private double? _cachedY;

        private static double ResolveValue(object v, double total)
        {
            switch (v)
            {
                case string s when s.EndsWith("%") &&
                                   double.TryParse(s.TrimEnd('%'), out var p):
                    {
                        double pct = p / 100.0;
                        if (pct >= 0)
                            return pct * total;
                        else
                            return total + (pct * total);
                    }

                case int i:
                    return i >= 0 ? i : total + i;

                case double d:
                    return d >= 0 ? d : total + d;

                default:
                    return 0;
            }
        }

        public (double X, double Y) Resolve(double totalWidth, double totalHeight, bool useCache = false)
        {
            bool isCacheBroken = _lastTotalWidth != totalWidth || _lastTotalHeight != totalHeight;

            if (useCache && _cachedX != null && _cachedY != null && !isCacheBroken)
                return ((double)_cachedX!, (double)_cachedY!);

            var newX = ResolveValue(X, totalWidth);
            var newY = ResolveValue(Y, totalHeight);

            if (useCache && isCacheBroken)
            {
                _lastTotalWidth = totalWidth;
                _lastTotalHeight = totalHeight;
                _cachedX = newX;
                _cachedY = newY;
            }

            return (
                newX,
                newY
            );
        }

        public override string ToString() => $"({X},{Y})";
    }
}