using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class FlexibleDimensionConverter : IValueConverter
    {
        public static readonly FlexibleDimensionConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is FlexibleDimension fd)
            {
                return fd.Value switch
                {
                    null => null,
                    double d => d.ToString(culture),
                    string s => s,
                    _ => fd.Value.ToString()
                };
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            if (value is FlexibleDimension fd)
                return fd;

            var str = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(str))
                return null;

            return new FlexibleDimension(str);
        }
    }
}
