using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    public class SimVarConfigConverter : JsonConverter<SimVarConfig>
    {
        public override SimVarConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of array for SimVarConfig");

            reader.Read();
            string? name = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

            reader.Read();
            string? unit = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

            reader.Read();

            if (name == null || unit == null)
                throw new Exception("Var name or unit is empty");

            return new SimVarConfig { Name = name, Unit = unit };
        }

        public override void Write(Utf8JsonWriter writer, SimVarConfig value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.Name);
            writer.WriteStringValue(value.Unit);
            writer.WriteEndArray();
        }
    }
}