using Microsoft.UI.Xaml.Data;
using PhotoViewer.App.Resources;
using PhotoViewer.Core.ViewModels;
using System;

namespace PhotoViewer.App.Converters;

public partial class AspectRadioModeToDisplayNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return (AspectRadioMode)value switch
        {
            AspectRadioMode.Orginal => Strings.AspectRadioMode_Orginal,
            AspectRadioMode.Free => Strings.AspectRadioMode_Free,
            AspectRadioMode.Fixed => Strings.AspectRadioMode_Fixed,
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}