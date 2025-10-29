using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Globalization;

namespace OpenGaugeClient
{
    public class ColorDefConverter : JsonConverter<ColorDef>
    {
        private static readonly Regex RgbaRegex = new(@"rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([\d.]+))?\)", RegexOptions.IgnoreCase);
        private static readonly Regex HslaRegex = new(@"hsla?\((\d+),\s*(\d+)%?,\s*(\d+)%?(?:,\s*([\d.]+))?\)", RegexOptions.IgnoreCase);

        public override ColorDef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString()!.Trim();

                if (str.StartsWith("#"))
                    return ColorDef.FromHex(str);

                var rgbaMatch = RgbaRegex.Match(str);
                if (rgbaMatch.Success)
                {
                    double alpha = 1.0;
                    if (rgbaMatch.Groups[4].Success)
                        alpha = double.Parse(rgbaMatch.Groups[4].Value, CultureInfo.InvariantCulture);

                    return new ColorDef(
                        int.Parse(rgbaMatch.Groups[1].Value),
                        int.Parse(rgbaMatch.Groups[2].Value),
                        int.Parse(rgbaMatch.Groups[3].Value),
                        alpha
                    );
                }

                var hslaMatch = HslaRegex.Match(str);
                if (hslaMatch.Success)
                {
                    double alpha = 1.0;
                    if (hslaMatch.Groups[4].Success)
                        alpha = double.Parse(hslaMatch.Groups[4].Value, CultureInfo.InvariantCulture);

                    return ColorDef.FromHsl(
                        double.Parse(hslaMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                        double.Parse(hslaMatch.Groups[2].Value, CultureInfo.InvariantCulture),
                        double.Parse(hslaMatch.Groups[3].Value, CultureInfo.InvariantCulture),
                        alpha
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