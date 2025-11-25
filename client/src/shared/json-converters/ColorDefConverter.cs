using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;

namespace OpenGaugeClient
{
    public class ColorDefConverter : JsonConverter<ColorDef>
    {
        public override ColorDef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString()!.Trim();

                if (Color.TryParse(str, out var avaloniaColor))
                {
                    return new ColorDef(
                        avaloniaColor.R,
                        avaloniaColor.G,
                        avaloniaColor.B,
                        avaloniaColor.A / 255.0
                    );
                }

                throw new JsonException($"Unsupported color format: {str}");
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                double[] values = JsonSerializer.Deserialize<double[]>(ref reader, options)!;
                if (values.Length is < 3 or > 4)
                    throw new JsonException("Color array must have 3 (RGB) or 4 (RGBA) values");

                double alpha = 1.0;
                if (values.Length == 4)
                    alpha = values[3] > 1 ? values[3] / 255.0 : values[3];

                return new ColorDef((int)values[0], (int)values[1], (int)values[2], alpha);
            }

            throw new JsonException("Invalid color format");
        }

        public override void Write(Utf8JsonWriter writer, ColorDef value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(
                value.A < 1.0
                    ? $"rgba({value.R},{value.G},{value.B},{value.A:0.##})"
                    : $"rgb({value.R},{value.G},{value.B})"
            );
        }
    }
}