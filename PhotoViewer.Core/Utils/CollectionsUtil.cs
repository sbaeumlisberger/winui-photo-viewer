using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PhotoViewer.App.Utils;

public static class CollectionsUtil
{
    public static bool IsEmpty<T>(this IEnumerable<T> collection)
    {
        return !collection.Any();
    }

    public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> source)
    {
        foreach (T item in source)
        {
            collection.Remove(item);
        }
    }

    public static bool RemoveFirst<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public static bool RemoveLast<T>(this IList<T> list)
    {
        if (list.IsEmpty()) 
        {
            return false;
        }
        list.RemoveAt(list.Count - 1);
        return true;
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var element in enumerable)
        {
            action(element);
        }
    }

    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable)
    {
        return enumerable.SelectMany(x => x);
    }

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> range)
    {
        range.ForEach(collection.Add);
    }

    public static List<T> ListOf<T>(T? element)
    {
        return element != null ? new List<T>(1) { element } : new List<T>(0);
    }

    /// <summary>Returns the successor of the given element or the deafault value if there is no successor.</summary>
    public static T? GetSuccessor<T>(this IReadOnlyList<T> list, T element)
    {
        var index = list.IndexOf(element);
        if (index == -1)
        {
            throw new ArgumentException(nameof(element));
        }
        if (index + 1 < list.Count)
        {
            return list[index + 1];
        }
        return default;
    }

    /// <summary>Returns the predecessor of the given element or the deafault value if there is no predecessor.</summary>
    public static T? GetPredecessor<T>(this IReadOnlyList<T> list, T element)
    {
        var index = list.IndexOf(element);
        if (index == -1)
        {
            throw new ArgumentException(nameof(element));
        }
        if (index - 1 > 0)
        {
            return list[index + 1];
        }
        return default;
    }

    public static int IndexOf<T>(this IReadOnlyList<T> list, T element)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (Equals(list[i], element))
            {
                return i;
            }
        }
        return -1;
    }

    public static List<T> GetNeighbours<T>(this IReadOnlyList<T> list, T element, int maxAmountPerSide = 1)
    {
        var index = list.IndexOf(element);
        if (index == -1)
        {
            throw new ArgumentException("The specified element was not found.", nameof(element));
        }
        int leftNeighboursCount = Math.Min(maxAmountPerSide, index);
        var leftNeighbours = list.Skip(index - leftNeighboursCount).Take(leftNeighboursCount);
        int rightNeighboursCount = Math.Min(maxAmountPerSide, list.Count - (index + 1));
        var rightNeighbours = list.Skip(index + 1).Take(rightNeighboursCount);
        return leftNeighbours.Concat(rightNeighbours).ToList();
    }

    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)  where TValue : new()
    {
        if (!dictionary.TryGetValue(key, out TValue? value))
        {
            value = new TValue();
            dictionary.Add(key, value);
        }
        return value;
    }
}
