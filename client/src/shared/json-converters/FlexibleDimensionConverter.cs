using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    public class FlexibleDimensionConverter : JsonConverter<FlexibleDimension>
    {
        public override FlexibleDimension Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            object? rawValue = reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString()!,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Invalid JSON token {reader.TokenType} for FlexibleDimension")
            };

            return new FlexibleDimension(rawValue!);
        }

        public override void Write(Utf8JsonWriter writer, FlexibleDimension value, JsonSerializerOptions options)
        {
            object v = value.Value;

            switch (v)
            {
                case null:
                    writer.WriteNullValue();
                    return;

                case string s:
                    writer.WriteStringValue(s);
                    return;

                case double d:
                    writer.WriteNumberValue(d);
                    return;

                case float f:
                    writer.WriteNumberValue(f);
                    return;

                case int i:
                    writer.WriteNumberValue(i);
                    return;

                case long l:
                    writer.WriteNumberValue(l);
                    return;

                default:
                    throw new JsonException($"Cannot write unsupported type {v.GetType()} from FlexibleDimension");
            }
        }
    }
}
