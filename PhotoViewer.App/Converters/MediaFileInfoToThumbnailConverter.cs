using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Threading.Tasks;

namespace PhotoViewer.App.Converters;

public class MediaFileInfoToThumbnailConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var bitmapImage = new BitmapImage();
        _ = TryLoadThumbnailAsync((IMediaFileInfo)value, bitmapImage);
        return bitmapImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    public static async Task<BitmapImage> ConvertAsync(IMediaFileInfo mediaFile) 
    {
        var bitmapImage = new BitmapImage();
        await TryLoadThumbnailAsync(mediaFile, bitmapImage);
        return bitmapImage;
    }

    private static async Task TryLoadThumbnailAsync(IMediaFileInfo mediaFile, BitmapImage bitmapImage)
    {
        try
        {
            bitmapImage.DecodePixelWidth = 512;

            using (var thumb = await mediaFile.GetThumbnailAsync())
            {
                if (thumb != null)
                {
                    await bitmapImage.SetSourceAsync(thumb);
                }
                else
                {
                    Log.Info("No thumbnail image associated with " + mediaFile.Name);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("No thumbnail could be be retrieved for " + mediaFile.Name, ex);
        }
    }

}