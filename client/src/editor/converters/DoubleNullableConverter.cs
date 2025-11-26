using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class DoubleNullableConverter : IValueConverter
    {
        public static readonly DoubleNullableConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() ?? "";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return null;

            return double.TryParse(s, NumberStyles.Any, culture, out var d)
                ? d
                : null;
        }
    }
}