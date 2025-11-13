using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OpenGaugeClient.Editor.Converters
{
    public class ColorDefConverter : IValueConverter
    {
        public static readonly ColorDefConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ColorDef def)
            {
                byte a = (byte)(def.A * 255);
                byte r = (byte)Math.Clamp(def.R, 0, 255);
                byte g = (byte)Math.Clamp(def.G, 0, 255);
                byte b = (byte)Math.Clamp(def.B, 0, 255);
                return Color.FromArgb(a, r, g, b);
            }
            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new ColorDef
                (
                    color.R,
                    color.G,
                    color.B,
                    color.A / 255.0
                );
            }
            return null;
        }
    }
}