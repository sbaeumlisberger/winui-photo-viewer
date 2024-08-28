using Microsoft.UI.Xaml.Data;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.Models;
using System;

namespace PhotoViewer.App.Converters;

public partial class AppThemeToDisplayNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return (AppTheme)value switch
        {
            AppTheme.System => Strings.AppTheme_System,
            AppTheme.Light => Strings.AppTheme_Light,
            AppTheme.Dark => Strings.AppTheme_Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}