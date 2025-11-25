using System.Text.Json;
using System.Text.Json.Serialization;

public class StringOrStringListConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new List<string> { reader.GetString()! };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType == JsonTokenType.String)
                    list.Add(reader.GetString()!);
                else
                    throw new JsonException("Expected string in array");
            }

            return list;
        }

        throw new JsonException("Expected string or array of strings");
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        if (value == null || value.Count == 0)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        if (value.Count == 1)
        {
            writer.WriteStringValue(value[0]);
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
            writer.WriteStringValue(item);
        writer.WriteEndArray();
    }
}
