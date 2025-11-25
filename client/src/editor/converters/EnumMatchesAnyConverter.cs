using System.Collections.Immutable;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class EnumMatchesAnyConverter : IMultiValueConverter
    {
        public IImmutableList<object> Targets { get; set; } = ImmutableList<object>.Empty;

        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0) return false;
            var value = values[0];
            return Targets.Contains(value);
        }
    }
}