
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class ObjectNullToggleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Avalonia.Data.BindingValue<bool> bindingVal && bindingVal.HasValue == false)
                return AvaloniaProperty.UnsetValue;

            if (value is not bool b)
                return AvaloniaProperty.UnsetValue;

            if (b)
            {
                if (parameter is Type type)
                    return Activator.CreateInstance(type);

                return AvaloniaProperty.UnsetValue;
            }

            return null;
        }
    }
}