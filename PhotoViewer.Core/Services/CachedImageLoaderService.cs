using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Services;

public interface ICachedImageLoaderService
{
    Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken, bool reload = false);

    void Preload(IBitmapFileInfo file);
}

internal class CachedImageLoaderService : ICachedImageLoaderService
{
    private const int CacheSize = 5;

    private readonly IImageLoaderService imageLoaderService;

    private readonly AsyncCache<IBitmapFileInfo, IBitmapImageModel> cache;

    public CachedImageLoaderService(IImageLoaderService imageLoaderService)
    {
        this.imageLoaderService = imageLoaderService;
        this.cache = new AsyncCache<IBitmapFileInfo, IBitmapImageModel>(CacheSize, image => image.Dispose());
    }

    public void Preload(IBitmapFileInfo file) 
    {
        Task.Run(() => cache.GetAsync(file, imageLoaderService.LoadFromFileAsync));
    }

    public async Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken, bool reload = false)
    {
        if (reload)
        {
            cache.Remove(file);
        }
        var image = await cache.GetAsync(file, imageLoaderService.LoadFromFileAsync, cancellationToken);
        image.RequestUsage();
        return image;
    }
}
