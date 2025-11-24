using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient.Editor
{
    public class ShadowUnionConverter : JsonConverter<ShadowUnion?>
    {
        public override ShadowUnion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Console.WriteLine($"READ {reader.TokenType}");
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    // reader.Read(); // consume 'true'
                    return new ShadowUnion { IsTrue = true };

                case JsonTokenType.False:
                    // reader.Read(); // consume 'false'
                    return null;

                case JsonTokenType.Null:
                    // reader.Read(); // consume 'null'
                    return null;

                case JsonTokenType.StartObject:
                    {
                        var config = JsonSerializer.Deserialize<ShadowConfig>(ref reader, options);

                        // IMPORTANT: advance past the EndObject token
                        // reader.Read();

                        return new ShadowUnion { Config = config };
                    }
            }

            throw new JsonException($"Unexpected token {reader.TokenType} for Shadow");
        }

        public override void Write(Utf8JsonWriter writer, ShadowUnion? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.IsTrue)
            {
                writer.WriteBooleanValue(true);
                return;
            }

            if (value.IsFalse)
            {
                writer.WriteBooleanValue(false);
                return;
            }

            if (value.Config is null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Config, options);
        }
    }
}