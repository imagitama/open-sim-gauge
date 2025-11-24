using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenGaugeClient.Editor.Converters
{
    public class EmptyListToBoolConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type t, object? p, CultureInfo c)
        {
            if (values.Count > 0 &&
                values[0] is int count)
            {
                return count == 0;
            }
            return false;
        }
    }
}