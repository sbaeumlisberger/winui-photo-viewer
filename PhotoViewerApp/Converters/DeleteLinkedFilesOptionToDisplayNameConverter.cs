using Microsoft.UI.Xaml.Data;
using PhotoViewerApp.Resources;
using PhotoViewerCore.Models;
using System;

namespace PhotoViewerApp.Converters;

public class DeleteLinkedFilesOptionToDisplayNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return (DeleteLinkedFilesOption)value switch
        {
            DeleteLinkedFilesOption.Ask => Strings.SettingsPage_DeleteLinkedFilesOption_Ask,
            DeleteLinkedFilesOption.Yes => Strings.SettingsPage_DeleteLinkedFilesOption_Yes,
            DeleteLinkedFilesOption.No => Strings.SettingsPage_DeleteLinkedFilesOption_No,
            _ => throw new Exception("Unknown option")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}