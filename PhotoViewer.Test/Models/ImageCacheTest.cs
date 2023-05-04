using NSubstitute;
using PhotoViewer.App.Models;
using PhotoViewer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PhotoViewer.Test.Models;

public class ImageCacheTest
{

    [Fact]
    public void ShoulValidateCacheSize()
    {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => new ImageCache(0));
    }

    [Fact]
    public void ShouldRequestImageWhenAdded()
    {
        var imageCache = new ImageCache(5);
        var file = Substitute.For<IBitmapFileInfo>();
        var image = Substitute.For<IBitmapImageModel>();
        imageCache.Set(file, image);

        image.Received().Request();
    }

    [Fact]
    public void ShouldReturnCachedImage()
    {
        var imageCache = new ImageCache(5);
        var file = Substitute.For<IBitmapFileInfo>();
        var image = Substitute.For<IBitmapImageModel>();
        imageCache.Set(file, image);

        bool found = imageCache.TryGet(file, out var cachedImage);

        Assert.True(found);
        Assert.Equal(image, cachedImage);
    }


    [Fact]
    public void ShouldReturnNothingWhenImageNotCached()
    {
        var imageCache = new ImageCache(5);
        var file = Substitute.For<IBitmapFileInfo>();

        bool found = imageCache.TryGet(file, out var cachedImage);

        Assert.False(found);
        Assert.Null(cachedImage);
    }

    [Fact]
    public void ShouldRemoveOldestImageWhenCacheSizeReached()
    {
        int cacheSize = 5;
        var imageCache = new ImageCache(cacheSize);
        var fistFile = Substitute.For<IBitmapFileInfo>();
        var firstImage = Substitute.For<IBitmapImageModel>();
        imageCache.Set(fistFile, firstImage);
        for (int i = 0; i < cacheSize; i++)
        {
            imageCache.Set(Substitute.For<IBitmapFileInfo>(), Substitute.For<IBitmapImageModel>());
        }

        bool found = imageCache.TryGet(fistFile, out var cachedImage);

        Assert.False(found);
        Assert.Null(cachedImage);
        firstImage.Received().Dispose();
    }

    [Fact]
    public void ShouldReplaceExstingCachedImageForSameFile()
    {
        int cacheSize = 5;
        var imageCache = new ImageCache(cacheSize);
        var file = Substitute.For<IBitmapFileInfo>();
        var image1 = Substitute.For<IBitmapImageModel>();
        imageCache.Set(file, image1);
        var image2 = Substitute.For<IBitmapImageModel>();
        imageCache.Set(file, image2);

        bool found = imageCache.TryGet(file, out var cachedImage);

        Assert.True(found);
        Assert.Equal(image2, cachedImage);
        image1.Received().Dispose();
        image2.Received().Request();
    }

    [Fact]
    public void ShouldHandleReplacedImageLikeNewAdded()
    {
        int cacheSize = 3;
        var imageCache = new ImageCache(cacheSize);
        var file = Substitute.For<IBitmapFileInfo>();
        imageCache.Set(file, Substitute.For<IBitmapImageModel>());
        imageCache.Set(Substitute.For<IBitmapFileInfo>(), Substitute.For<IBitmapImageModel>());
        imageCache.Set(Substitute.For<IBitmapFileInfo>(), Substitute.For<IBitmapImageModel>());
        imageCache.Set(file, Substitute.For<IBitmapImageModel>());
        imageCache.Set(Substitute.For<IBitmapFileInfo>(), Substitute.For<IBitmapImageModel>());

        bool found = imageCache.TryGet(file, out var cachedImage);

        Assert.True(found);
        Assert.NotNull(cachedImage);
    }
}
