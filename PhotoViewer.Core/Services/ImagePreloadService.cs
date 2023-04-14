using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

internal class ImagePreloadService
{
    public static ImagePreloadService Instance { get; } = new ImagePreloadService();

    private readonly IImageLoaderService imageLoaderService = new ImageLoaderService(new GifImageLoaderService());

    private IBitmapFileInfo? preloadFile;

    private Task<IBitmapImageModel>? preloadTask;

    public void Preload(IBitmapFileInfo file)
    {
        preloadFile = file;
        preloadTask = imageLoaderService.LoadFromFileAsync(file, CancellationToken.None);
    }

    public async Task<IBitmapImageModel?> GetPreloadedImageAsync(IBitmapFileInfo file)
    {
        if (file.Equals(preloadFile))
        {
            var bitmapImage = await preloadTask!;
            preloadFile = null;
            preloadTask = null;
            return bitmapImage;
        }
        return null;
    }

}
