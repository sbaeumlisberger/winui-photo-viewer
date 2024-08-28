using Microsoft.UI.Xaml.Data;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.ViewModels;
using System;

namespace PhotoViewer.App.Converters;

public partial class ItemCountToStringConverter : IValueConverter
{

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ItemWithCountModel model)
        {
            return "(" + (model.Count == model.Total ? Strings.ItemWithCount_All : model.Count) + ")";
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}