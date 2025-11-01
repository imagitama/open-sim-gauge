using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    public class FlexibleVector2
    {
        public object X { get; set; } = 0;
        public object Y { get; set; } = 0;

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
                    return (p / 100.0) * total;
                case int i:
                    return i;
                case double d:
                    return d;
                default:
                    return 0;
            }
        }

        public (double X, double Y) Resolve(double totalWidth, double totalHeight, bool useCache = true)
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

        public override string ToString() => $"({X}, {Y})";
    }

    public class FlexibleVector2Converter : JsonConverter<FlexibleVector2>
    {
        public override FlexibleVector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected [x, y] array");

            reader.Read();
            object x = reader.TokenType == JsonTokenType.String ? reader.GetString()! : reader.GetDouble();

            reader.Read();
            object y = reader.TokenType == JsonTokenType.String ? reader.GetString()! : reader.GetDouble();

            reader.Read();
            return new FlexibleVector2 { X = x, Y = y };
        }

        public override void Write(Utf8JsonWriter writer, FlexibleVector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            if (value.X is string xs) writer.WriteStringValue(xs);
            else if (value.X is double xd) writer.WriteNumberValue(xd);
            if (value.Y is string ys) writer.WriteStringValue(ys);
            else if (value.Y is double yd) writer.WriteNumberValue(yd);
            writer.WriteEndArray();
        }
    }
}
