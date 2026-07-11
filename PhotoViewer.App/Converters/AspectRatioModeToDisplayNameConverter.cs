using Microsoft.UI.Xaml.Data;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.ViewModels;
using System;

namespace PhotoViewer.App.Converters;

public partial class AspectRatioModeToDisplayNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return (AspectRatioMode)value switch
        {
            AspectRatioMode.Original => Strings.AspectRatioMode_Original,
            AspectRatioMode.Free => Strings.AspectRatioMode_Free,
            AspectRatioMode.Fixed => Strings.AspectRatioMode_Fixed,
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}
