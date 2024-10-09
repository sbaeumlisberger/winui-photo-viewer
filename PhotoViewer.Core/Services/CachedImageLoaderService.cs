using Essentials.NET;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Services;

public interface ICachedImageLoaderService
{
    Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken, bool reload = false);

    void Preload(string filePath);
}

public class CachedImageLoaderService : ICachedImageLoaderService
{
    public static readonly ICachedImageLoaderService Instance = new CachedImageLoaderService(new ImageLoaderService(new GifImageLoaderService()));

    private const int CacheSize = 5;

    private readonly IImageLoaderService imageLoaderService;

    private readonly AsyncCache<ulong, IBitmapImageModel> cache;

    private Task preloadTask = Task.CompletedTask;

    public CachedImageLoaderService(IImageLoaderService imageLoaderService)
    {
        this.imageLoaderService = imageLoaderService;
        this.cache = new AsyncCache<ulong, IBitmapImageModel>(CacheSize, CacheSize, image => image.Dispose(), image => image.RequestUsage());
    }

    public void Preload(string filePath)
    {
        ulong id = MediaFileInfoBase.GetIdForFilePath(filePath);
        preloadTask = cache.GetOrCreateAsync(id, (_, cancellationToken) => imageLoaderService.LoadFromFileAsync(filePath, cancellationToken));
    }

    public async Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken, bool reload = false)
    {
        if (!preloadTask.IsCompleted)
        {
            await preloadTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            preloadTask = Task.CompletedTask;
        }

        if (reload)
        {
            cache.Remove(file.Id);
        }

        return await cache.GetOrCreateAsync(file.Id, (_, cancellationToken) => imageLoaderService.LoadFromFileAsync(file, cancellationToken), cancellationToken).ConfigureAwait(false);
    }
}
