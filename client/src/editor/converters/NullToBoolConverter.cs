
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class NullToBoolConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Invert ? value == null : value != null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => AvaloniaProperty.UnsetValue;
    }
}