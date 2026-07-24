using OrderHub.UI.Features.Settings.Properties.PropertyEditor;
using System;
using System.Globalization;
using System.Windows.Data;

namespace OrderHub.UI.Converters;
public class ReadOnlyModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Mode.ReadOnly;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}