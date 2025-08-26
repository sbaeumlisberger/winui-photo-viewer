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

    private readonly AsyncCache<string, IBitmapImageModel> cache;

    private Task preloadTask = Task.CompletedTask;

    public CachedImageLoaderService(IImageLoaderService imageLoaderService)
    {
        this.imageLoaderService = imageLoaderService;
        this.cache = new AsyncCache<string, IBitmapImageModel>(CacheSize, CacheSize, image => image.Dispose(), image => image.RequestUsage());
    }

    public void Preload(string filePath)
    {
        preloadTask = cache.GetOrCreateAsync(Path.GetFileName(filePath), (_, cancellationToken) => imageLoaderService.LoadFromFileAsync(filePath, cancellationToken));
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
            cache.Remove(file.FileName);
        }

        return await cache.GetOrCreateAsync(file.FileName, (_, cancellationToken) => imageLoaderService.LoadFromFileAsync(file, cancellationToken), cancellationToken).ConfigureAwait(false);
    }
}
