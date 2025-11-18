using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
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
