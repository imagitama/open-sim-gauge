using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient
{
    [JsonConverter(typeof(Vector2Converter))]
    public struct Vector2
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly int[] ToArray() => [X, Y];

        public override readonly string ToString() => $"[{X}, {Y}]";
    }

    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var arr = JsonSerializer.Deserialize<int[]>(ref reader, options)!;
                if (arr.Length != 2)
                    throw new JsonException("Position must contain exactly 2 integers [x, y].");
                return new Vector2(arr[0], arr[1]);
            }

            throw new JsonException("Invalid format for Vector2.");
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
}
