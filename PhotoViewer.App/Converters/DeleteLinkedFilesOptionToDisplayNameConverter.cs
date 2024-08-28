using Microsoft.UI.Xaml.Data;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.Models;
using System;

namespace PhotoViewer.App.Converters;

public partial class DeleteLinkedFilesOptionToDisplayNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return (DeleteLinkedFilesOption)value switch
        {
            DeleteLinkedFilesOption.Ask => Strings.SettingsPage_DeleteLinkedFilesOption_Ask,
            DeleteLinkedFilesOption.Yes => Strings.SettingsPage_DeleteLinkedFilesOption_Yes,
            DeleteLinkedFilesOption.No => Strings.SettingsPage_DeleteLinkedFilesOption_No,
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}