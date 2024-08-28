using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Graphics.Imaging;

namespace PhotoViewer.App.Converters;

public partial class SoftwareBitmapToImageSourceConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var softwareBitmapSource = new SoftwareBitmapSource();
        _ = softwareBitmapSource.SetBitmapAsync((SoftwareBitmap)value);
        return softwareBitmapSource;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
