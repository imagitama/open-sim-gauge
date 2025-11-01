using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
public class VarConfigConverter : JsonConverter<VarConfig>
{
    public override VarConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for VarConfig");

        reader.Read();
        string? name = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        reader.Read();
        string? unit = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        reader.Read();

        if (name == null || unit == null)
            throw new Exception("Var name or unit is empty");

        return new VarConfig { Name = name, Unit = unit };
    }

    public override void Write(Utf8JsonWriter writer, VarConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Name);
        writer.WriteStringValue(value.Unit);
        writer.WriteEndArray();
    }
}
}