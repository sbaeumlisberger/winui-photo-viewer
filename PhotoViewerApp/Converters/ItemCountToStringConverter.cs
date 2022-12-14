using Microsoft.UI.Xaml.Data;
using PhotoViewerApp.Resources;
using PhotoViewerCore.ViewModels;
using System;

namespace PhotoViewerApp.Converters;

public class ItemCountToStringConverter : IValueConverter
{

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var model = (ItemWithCountModel)value;
        return "(" + (model.Count == model.Total ? Strings.ItemWithCount_All : model.Count) + ")";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}