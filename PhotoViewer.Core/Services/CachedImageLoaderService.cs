using Castle.Components.DictionaryAdapter.Xml;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tocronx.SimpleAsync;
using static PhotoViewer.Core.Models.ImageCache;

namespace PhotoViewer.Core.Services;

public interface ICachedImageLoaderService
{
    Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken, bool reload = false);
}

internal class CachedImageLoaderService : ICachedImageLoaderService
{
    private const int CacheSize = 5;

    public static ICachedImageLoaderService Instance = new CachedImageLoaderService(new ImageLoaderService(new GifImageLoaderService()));

    private readonly IImageLoaderService imageLoaderService;

    private readonly ImageCache cache;

    private readonly Dictionary<IBitmapFileInfo, SharedTask<IBitmapImageModel>> tasks = new();

    public CachedImageLoaderService(IImageLoaderService imageLoaderService)
    {
        this.imageLoaderService = imageLoaderService;
        this.cache = new ImageCache(CacheSize);
    }

    public async Task<IBitmapImageModel> LoadFromFileAsync(IBitmapFileInfo file, CancellationToken cancellationToken, bool reload = false)
    {
        if (!reload)
        {
            if (tasks.TryGetValue(file, out var existingTask))
            {
                var image = await existingTask.GetTask(cancellationToken);
                image.Request();
                return image;
            }
            if (cache.TryGet(file, out var cachedImage))
            {
                cachedImage.Request();
                return cachedImage;
            }
        }

        var sharedTask = new SharedTask<IBitmapImageModel>();
        tasks[file] = sharedTask;
        sharedTask.Start(async cancellationToken =>
        {
            try
            {
                var image = await imageLoaderService.LoadFromFileAsync(file, cancellationToken);
                cache.Set(file, image);
                return image;
            }
            finally
            {
                tasks.Remove(file);
            }
        });       

        return await sharedTask.GetTask(cancellationToken);
    }
}
