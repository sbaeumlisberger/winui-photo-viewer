using Microsoft.UI.Xaml.Data;
using System;

namespace PhotoViewer.App.Converters;

public partial class TimeSpanToDoubleConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is TimeSpan timeSpan ? timeSpan.TotalSeconds : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is double doubleValue && !double.IsNaN(doubleValue) ? TimeSpan.FromSeconds(doubleValue) : TimeSpan.Zero;
    }

}