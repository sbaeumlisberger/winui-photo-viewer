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

    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable)
    {
        return enumerable.SelectMany(x => x);
    }

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> range)
    {
        range.ForEach(collection.Add);
    }

    public static List<T> NotNull<T>(T? element)
    {
        return element != null ? new List<T>(1) { element } : new List<T>(0);
    }
}
