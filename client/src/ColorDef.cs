using System;
using System.Globalization;
using Avalonia.Media;

namespace OpenGaugeClient
{
    public class ColorDef
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public double A { get; set; }

        public ColorDef(int r, int g, int b, double a = 1.0)
        {
            R = r; G = g; B = b; A = a;
        }

        public override string ToString() => $"rgba({R},{G},{B},{A:0.##})";

        public static ColorDef FromHex(string hex)
        {
            hex = hex.TrimStart('#');

            if (hex.Length == 3)
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
            else if (hex.Length == 4)
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2], hex[3], hex[3]);

            if (hex.Length == 6)
            {
                return new ColorDef(
                    int.Parse(hex[..2], NumberStyles.HexNumber),
                    int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                    int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber)
                );
            }
            else if (hex.Length == 8)
            {
                return new ColorDef(
                    int.Parse(hex[..2], NumberStyles.HexNumber),
                    int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                    int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber),
                    int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) / 255.0
                );
            }

            throw new FormatException($"Invalid hex color: {hex}");
        }

        public static ColorDef FromHsl(double h, double s, double l, double a = 1.0)
        {
            s /= 100.0;
            l /= 100.0;
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2;
            double r = 0, g = 0, b = 0;

            if (h < 60)      (r, g, b) = (c, x, 0);
            else if (h < 120)(r, g, b) = (x, c, 0);
            else if (h < 180)(r, g, b) = (0, c, x);
            else if (h < 240)(r, g, b) = (0, x, c);
            else if (h < 300)(r, g, b) = (x, 0, c);
            else              (r, g, b) = (c, 0, x);

            return new ColorDef(
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255),
                a
            );
        }

        public Color ToColor()
        {
            return Color.FromRgb((byte)R, (byte)G, (byte)B);
        }
    }
}