using Microsoft.UI.Xaml.Data;
using System;
using Windows.Storage;

namespace PhotoViewerApp.Converters;

public class TimeSpanToDoubleConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is TimeSpan timeSpan ? timeSpan.TotalSeconds : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is double doubleValue ? TimeSpan.FromSeconds(doubleValue) : TimeSpan.Zero;
    }

}