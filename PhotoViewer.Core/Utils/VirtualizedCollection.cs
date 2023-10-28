using Microsoft.UI.Xaml.Controls.Primitives;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

internal class VirtualizedCollection<TKey, TValue> where TKey : notnull
{
    public IReadOnlyList<TValue> RealizedValues => cache.Values.ToList();

    private readonly Dictionary<TKey, TValue> cache = new();

    private IReadOnlyList<TKey> keys;

    private readonly int cacheSizePerSide;

    private readonly Func<TKey, TValue> load;
    private readonly Action<TValue> cleanup;

    public VirtualizedCollection(IReadOnlyList<TKey> keys, int cacheSizePerSide, Func<TKey, TValue> load, Action<TValue> cleanup)
    {
        this.keys = keys;
        this.cacheSizePerSide = cacheSizePerSide;
        this.load = load;
        this.cleanup = cleanup;
    }

    public void Clear()
    {
        cache.Values.ForEach(cleanup);
        cache.Clear();
    }

    public void SetKeys(IReadOnlyList<TKey> keys) 
    {
        this.keys = keys;
    }

    public TValue? SetSelectedItem(TKey? selectedItem)
    {
        var keysThatShouldBeCached = selectedItem is not null
           ? keys.GetNeighbours(selectedItem, cacheSizePerSide).Prepend(selectedItem).ToList()
           : new List<TKey>();

        var keysToCache = keysThatShouldBeCached.Except(cache.Keys).ToList();
        var keysToCleanup = cache.Keys.Except(keysThatShouldBeCached);

        foreach (var key in keysToCache)
        {
            var value = load(key);
            cache.Add(key, value);
        }

        foreach (var key in keysToCleanup)
        {
            cleanup(cache[key]);
            cache.Remove(key);
        }
 
        return selectedItem is not null ? cache[selectedItem] : default;
    }

    public TValue GetValue(TKey key)
    {
        return cache[key];
    }
}
