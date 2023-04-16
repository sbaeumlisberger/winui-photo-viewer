using PhotoViewer.Core.Utils;

namespace PhotoViewer.App.Utils;

public static class CollectionsUtil
{
    public static void RemoveRange<TValue>(this ICollection<TValue> collection, IEnumerable<TValue> source)
    {
        foreach (TValue item in source)
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

    // TODO add test
    public static void MatchTo<T>(this IList<T> list, IReadOnlyList<T> other)
    {
        list.RemoveRange(list.Except(other).ToList());

        for (int i = 0; i < other.Count; i++)
        {
            if (i > list.Count - 1)
            {
                list.Add(other[i]);
            }
            else if (!Equals(other[i], list[i]))
            {
                int oldIndex = list.IndexOf(other[i]);
                if (oldIndex != -1)
                {
                    list.RemoveAt(oldIndex);               
                }
                list.Insert(i, other[i]);                
            }
        }
    }
}
