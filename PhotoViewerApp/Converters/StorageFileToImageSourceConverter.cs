using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewerApp.Utils.Logging;
using System;
using System.Diagnostics;
using Windows.Storage;

namespace PhotoViewerApp.Converters;

public class StorageFileToImageSourceConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        BitmapImage imgSource = new BitmapImage();
        if (value is IStorageFile file)
        {
            TryLoadAsync(file, imgSource);
        }
        return imgSource;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    public static async void TryLoadAsync(IStorageFile file, BitmapImage imgSource)
    {
        try
        {
            using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                await imgSource.SetSourceAsync(fileStream);
            }
        }
        catch (Exception ex)
        {
            Log.Error("No image could be loaded for " + file.Name, ex);
        }
    }
}