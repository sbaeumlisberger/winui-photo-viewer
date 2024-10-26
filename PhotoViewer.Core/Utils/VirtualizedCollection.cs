using Essentials.NET;
using System.Collections.Specialized;

namespace PhotoViewer.Core.Utils;

internal static class VirtualizedCollection
{
    public static VirtualizedCollection<TKey, TValue> Create<TKey, TValue>(
        int cacheSizePerSide, Func<TKey, TValue> load, Action<TValue> cleanup, IObservableReadOnlyList<TKey> keys) where TKey : notnull
    {
        return new VirtualizedCollection<TKey, TValue>(cacheSizePerSide, load, cleanup, keys);
    }
}

internal class VirtualizedCollection<TKey, TValue> where TKey : notnull
{
    public IReadOnlyList<TValue> Values => cache.Values.ToList();

    private readonly Dictionary<TKey, TValue> cache = new();

    private IObservableReadOnlyList<TKey> keys;

    private TKey? selectedItem;

    private readonly int cacheSizePerSide;

    private readonly Func<TKey, TValue> load;
    private readonly Action<TValue> cleanup;

    public VirtualizedCollection(int cacheSizePerSide, Func<TKey, TValue> load, Action<TValue> cleanup, IObservableReadOnlyList<TKey> keys)
    {
        this.cacheSizePerSide = cacheSizePerSide;
        this.load = load;
        this.cleanup = cleanup;
        this.keys = keys;
        this.keys.CollectionChanged += Keys_CollectionChanged;
    }

    public void ClearCache()
    {
        cache.Values.ForEach(cleanup);
        cache.Clear();
    }

    public void SetKeys(IObservableReadOnlyList<TKey> keys)
    {
        this.keys.CollectionChanged -= Keys_CollectionChanged;
        this.keys = keys;
        this.keys.CollectionChanged += Keys_CollectionChanged;
        UpdateCache();
    }

    public TValue? SetSelectedItem(TKey? selectedItem)
    {
        this.selectedItem = selectedItem;
        UpdateCache();
        return selectedItem is not null ? GetValue(selectedItem) : default;
    }

    public TValue GetValue(TKey key)
    {
        return cache[key];
    }

    private void Keys_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCache();
    }

    private void UpdateCache() 
    {
        if (selectedItem is null)
        {
            ClearCache();
            return;
        }
        else
        {
            if (!cache.ContainsKey(selectedItem))
            {
                cache.Add(selectedItem, load(selectedItem));
            }

            var keysToCache = GetKeysToCache(selectedItem);

            foreach (var key in keysToCache)
            {
                if (!cache.ContainsKey(key))
                {
                    cache.Add(key, load(key));
                }
            }

            foreach (var key in cache.Keys.Except(keysToCache).ToList())
            {
                cleanup(cache[key]);
                cache.Remove(key);
            }
        }
    }

    private List<TKey> GetKeysToCache(TKey selectedItem)
    {
        int index = keys.IndexOf(selectedItem);
        int skipCount = Math.Max(index - cacheSizePerSide, 0);
        int takeCount = Math.Min(cacheSizePerSide, index) + 1 + cacheSizePerSide;
        return keys.Skip(skipCount).Take(takeCount).ToList();
    }
}
