using System;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.Converters;

public class CanvasBitmapToImageSourceConverter : IValueConverter
{

    private CanvasImageSource? imageSource; // TODO cleanup?

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var image = (IBitmapImage)value;

        if (image is null)
        {
            return null;
        }

        var device = CanvasDevice.GetSharedDevice();

        if (imageSource is null || image.SizeInDIPs.Width != imageSource.Size.Width || image.SizeInDIPs.Height != imageSource.Size.Height)
        {
            // TODO CancasVirutalImageSource
            imageSource = new CanvasImageSource(device, (float)image.SizeInDIPs.Width, (float)image.SizeInDIPs.Height, 96);
        }

        using var drawingSession = imageSource.CreateDrawingSession(Colors.Transparent);

        ColorManagementProfile? sourceColorProfile = null;

        if (image.ColorSpace.Profile is byte[] colorProfileBytes)
        {
            sourceColorProfile = ColorManagementProfile.CreateCustom(colorProfileBytes);
        }

        var outputColorProfile = ColorProfileProvider.GetColorProfile();

        if (sourceColorProfile is not null || outputColorProfile is not null)
        {
            using var colorProfileEffect = new ColorManagementEffect()
            {
                Source = image.CanvasImage,
                SourceColorProfile = sourceColorProfile,
                OutputColorProfile = outputColorProfile
            };

            if (ColorManagementEffect.IsBestQualitySupported(device))
            {
                colorProfileEffect.Quality = ColorManagementEffectQuality.Best;
            }      

            drawingSession.DrawImage(colorProfileEffect);
        }
        else
        {          
            drawingSession.DrawImage(image.CanvasImage);
        }

        return imageSource;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

}

