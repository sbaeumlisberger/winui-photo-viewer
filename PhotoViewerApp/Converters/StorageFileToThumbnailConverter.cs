using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils.Logging;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace PhotoViewerApp.Converters;

public class StorageFileToThumbnailConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        BitmapImage bitmapImage = new BitmapImage();
        _ = TryLoadThumbnailAsync((StorageFile)value, bitmapImage);
        return bitmapImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static async Task TryLoadThumbnailAsync(StorageFile file, BitmapImage bitmapImage)
    {
        try
        {
            if (BitmapFileInfo.SupportedFileExtensions.Contains(file.FileType.ToLower()))
            {
                // loading the image directly is much faster and always up-to-date

                using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    await bitmapImage.SetSourceAsync(fileStream);
                }
            }
            else
            {
                using (var thumb = await file.GetThumbnailAsync(ThumbnailMode.SingleItem))
                {
                    if (thumb != null)
                    {
                        await bitmapImage.SetSourceAsync(thumb);
                    }
                    else
                    {
                        Log.Info("No thumbnail image associated with " + file.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Info("No thumbnail could be be retrieved for " + file.Name + " (" + ex.Message + ")");
        }
    }

}