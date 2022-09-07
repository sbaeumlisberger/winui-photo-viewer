using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PhotoViewerApp.Utils;

public static class CollectionsUtil
{

    /// <summary>Removes all the elements that match the conditions defined by the specified predicate.</summary>
    public static void RemoveAll<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
            }
        }
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var element in enumerable) 
        { 
            action(element); 
        }
    }

    public static void AddRange<T>(this ICollection<T> collection, IList<T> range)
    {
        range.ForEach(collection.Add);
    }
}
