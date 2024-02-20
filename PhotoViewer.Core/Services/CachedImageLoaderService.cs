using Essentials.NET;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;

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
        this.cache = new AsyncCache<IBitmapFileInfo, IBitmapImageModel>(CacheSize, image => image.Dispose(), image => image.RequestUsage());
    }

    public void Preload(IBitmapFileInfo file)
    {
        Task.Run(() => cache.GetOrCreateAsync(file, imageLoaderService.LoadFromFileAsync));
    }

    public async Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken, bool reload = false)
    {
        if (reload)
        {
            cache.Remove(file);
        }
        return await cache.GetOrCreateAsync(file, imageLoaderService.LoadFromFileAsync, cancellationToken);
    }
}
