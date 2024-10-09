using Essentials.NET.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewer.Core.Models;
using System;
using System.Threading.Tasks;

namespace PhotoViewer.App.Converters;

public class MediaFileInfoToThumbnailConverter
{
    private const int DecodePixelWidth = 512;

    public static BitmapImage Convert(IMediaFileInfo mediaFile)
    {
        var bitmapImage = new BitmapImage();
        _ = TryLoadThumbnailAsync(mediaFile, bitmapImage);
        return bitmapImage;
    }

    private static async Task TryLoadThumbnailAsync(IMediaFileInfo mediaFile, BitmapImage bitmapImage)
    {
        try
        {
            bitmapImage.DecodePixelWidth = DecodePixelWidth;

            using (var thumb = await mediaFile.GetThumbnailAsync())
            {
                if (thumb != null)
                {
                    await bitmapImage.SetSourceAsync(thumb);
                }
                else
                {
                    Log.Info("No thumbnail image associated with " + mediaFile.DisplayName);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("No thumbnail could be be retrieved for " + mediaFile.DisplayName, ex);
        }
    }

}