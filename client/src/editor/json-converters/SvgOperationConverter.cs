using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenGaugeClient.Editor
{
    public sealed class SvgOperationConverter : JsonConverter<SvgOperation>
    {
        public override SvgOperation Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                throw new JsonException("Missing 'type' discriminator");

            string typeString = typeProp.GetString()!;
            var parsed = Enum.Parse<SvgOperationType>(typeString, ignoreCase: true);

            Type actualType = parsed switch
            {
                SvgOperationType.Circle => typeof(CircleSvgOperation),
                SvgOperationType.Triangle => typeof(TriangleSvgOperation),
                SvgOperationType.Square => typeof(SquareSvgOperation),
                SvgOperationType.Arc => typeof(ArcSvgOperation),
                SvgOperationType.GaugeTickLabels => typeof(GaugeTickLabelsSvgOperation),
                SvgOperationType.GaugeTicks => typeof(GaugeTicksSvgOperation),
                SvgOperationType.Text => typeof(TextSvgOperation),
                _ => throw new JsonException($"Unknown SvgOperation type '{typeString}'")
            };

            string raw = root.GetRawText();
            return (SvgOperation)JsonSerializer.Deserialize(raw, actualType, options)!;
        }

        public override void Write(
        Utf8JsonWriter writer,
        SvgOperation value,
        JsonSerializerOptions options)
        {
            var innerOptions = new JsonSerializerOptions(options);
            for (int i = innerOptions.Converters.Count - 1; i >= 0; i--)
            {
                if (innerOptions.Converters[i] is SvgOperationConverter ||
                    innerOptions.Converters[i] is JsonConverter<SvgOperation>)
                {
                    innerOptions.Converters.RemoveAt(i);
                }
            }

            string json = JsonSerializer.Serialize(value, value.GetType(), innerOptions);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            writer.WriteStartObject();

            writer.WriteString("type", value.Type.ToString());

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("type"))
                    continue;

                prop.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}