using System;
using System.Globalization;
using System.Windows.Data;

namespace OrderHub.UI.Converters;
public sealed class UtcToLocalTimeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value switch
        {
            DateTime dateTime => dateTime.ToLocalTime(),
            _ => value
        };
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}