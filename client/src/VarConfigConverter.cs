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
        var name = reader.GetString() ?? "";
        reader.Read();
        var unit = reader.GetString() ?? "";

        VarConfigOptions? opts = null;

        reader.Read();
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            opts = JsonSerializer.Deserialize<VarConfigOptions>(ref reader, options);
            reader.Read();
        }

        if (reader.TokenType != JsonTokenType.EndArray)
            reader.Read();

        return new VarConfig { Name = name, Unit = unit, Options = opts };
    }

    public override void Write(Utf8JsonWriter writer, VarConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Name);
        writer.WriteStringValue(value.Unit);
        if (value.Options != null)
            JsonSerializer.Serialize(writer, value.Options, options);
        writer.WriteEndArray();
    }
}
}