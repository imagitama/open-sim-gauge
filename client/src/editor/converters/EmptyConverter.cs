using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class EmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return "";

            return value.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var s = value as string;

            if (string.IsNullOrWhiteSpace(s))
            {
                if (parameter != null)
                    return parameter;

                return null;
            }

            return s;
        }
    }
}