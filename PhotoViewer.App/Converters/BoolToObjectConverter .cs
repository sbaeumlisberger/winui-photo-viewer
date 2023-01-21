using Microsoft.UI.Xaml.Data;
using System;

namespace PhotoViewerApp.Converters;

public class BoolToObjectConverter : IValueConverter
{
    public object? TrueValue { get; set; }
    public object? FalseValue { get; set; }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ((bool)value) ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}