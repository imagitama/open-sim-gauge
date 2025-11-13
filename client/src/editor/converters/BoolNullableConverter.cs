using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class BoolNullableConverter : IValueConverter
    {
        public static readonly BoolNullableConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return (bool?)b;

            if (value is bool?)
                return (bool?)value;

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            Console.WriteLine($"ConvertBack value={value} targetType={targetType}");

            if (value is bool b)
                return (bool?)b;

            if (value is bool?)
                return (bool?)value ?? false;

            return false;
        }
    }
}