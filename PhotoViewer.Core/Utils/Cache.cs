using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

internal class Cache<TKey, TValue> where TKey : notnull
{
    public int MaxSize { get; }

    public int CurrentSize => cacheList.Count;

    public IEnumerable<TValue> Values => cacheList.Select(x => x.Value);

    private readonly Dictionary<TKey, TValue> cacheDictionary;

    private readonly List<(TKey Key, TValue Value)> cacheList;

    private readonly Action<TValue>? removedCallback;

    public Cache(int maxSize, Action<TValue>? removedCallback = null) 
    {
        if (maxSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSize));
        }
        MaxSize = maxSize;
        this.removedCallback = removedCallback;
        cacheDictionary = new Dictionary<TKey, TValue>(maxSize);
        cacheList = new List<(TKey, TValue)>(maxSize);
    }

    public bool Remove(TKey key)
    {
        lock (cacheDictionary)
        {
            if (cacheDictionary.TryGetValue(key, out var value))
            {
                cacheDictionary.Remove(key);
                cacheList.RemoveAt(cacheList.FindIndex(entry => Equals(entry.Key, key)));
                removedCallback?.Invoke(value);
                return true;
            }
            return false;
        }
    }

    public TValue GetOrCreate(TKey key, Func<TKey, TValue> createValueCallback)
    {
        lock (cacheDictionary)
        {
            if (cacheDictionary.TryGetValue(key, out var value))
            {
                int index = cacheList.FindIndex(entry => Equals(entry.Key, key));
                if (index != cacheList.Count - 1) 
                {
                    cacheList.RemoveAt(index);
                    cacheList.Add((key, value));
                }
            }
            else
            {
                if (cacheList.Count == MaxSize)
                {
                    RemoveOldestCacheEntry();
                }
                value = createValueCallback.Invoke(key);
                cacheDictionary[key] = value;
                cacheList.Add((key, value));
            }
            return value;
        }
    }

    private void RemoveOldestCacheEntry()
    {
        var cacheEntry = cacheList[0];

        cacheDictionary.Remove(cacheEntry.Key);
        cacheList.RemoveAt(0);

        removedCallback?.Invoke(cacheEntry.Value);
    }

}
