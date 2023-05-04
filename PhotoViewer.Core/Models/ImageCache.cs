using PhotoViewer.App.Models;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Models;

public class ImageCache
{
    private readonly Dictionary<IBitmapFileInfo, IBitmapImageModel> cacheMap;

    private readonly List<(IBitmapFileInfo File, IBitmapImageModel Image)> cacheList;

    private readonly int cacheSize;

    public ImageCache(int cacheSize)
    {
        if (cacheSize < 1) 
        {
            throw new ArgumentOutOfRangeException(nameof(cacheSize));
        }
        this.cacheSize = cacheSize;
        cacheMap = new Dictionary<IBitmapFileInfo, IBitmapImageModel>(cacheSize);
        cacheList = new List<(IBitmapFileInfo, IBitmapImageModel)>(cacheSize);
    }

    public void Set(IBitmapFileInfo file, IBitmapImageModel image)
    {
        bool existing = cacheMap.ContainsKey(file);

        if (!existing && cacheList.Count == cacheSize)
        {
            var first = cacheList.First();
            Log.Debug("Remove from image cache " + first);
            first.Image.Dispose();
            cacheList.Remove(first);
            cacheMap.Remove(first.File);
        }

        if (existing)
        {
            cacheMap[file].Dispose();
            cacheList.Remove(cacheList.First(entry => entry.File.Equals(file)));
        }
        image.Request();
        cacheList.Add((file, image));
        cacheMap[file] = image;
    }

    public bool TryGet(IBitmapFileInfo file, [NotNullWhen(true)] out IBitmapImageModel? image)
    {
        if (cacheMap.TryGetValue(file, out var cacheEntry))
        {
            image = cacheEntry;
            return true;
        }
        image = null;
        return false;
    }

}
