using System;
using System.Collections.Generic;
using System.Threading;

namespace PhotoViewerApp.Utils;

internal static class CollectionsUtil
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

}
