using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public sealed class CommaSeparatedStringConverter : IValueConverter
    {
        public static readonly CommaSeparatedStringConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable<string> list)
                return string.Join(", ", list);

            return "";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
            }

            return new List<string>();
        }
    }
}