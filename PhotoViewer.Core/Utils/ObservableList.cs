using PhotoViewer.App.Utils;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PhotoViewer.Core.Utils;

public interface IObservableReadOnlyList<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    bool Contains(T value);

    int IndexOf(T value);
}

public interface IObservableList<T> : IObservableReadOnlyList<T>, IList<T>
{
    void Move(int oldIndex, int newIndex);
}

public class ObservableList<T> : ObservableCollection<T>, IObservableList<T>, IObservableReadOnlyList<T>
{
    public ObservableList() { }

    public ObservableList(IEnumerable<T> enumerable) : base(enumerable) { }

    public void MatchTo(IList<T> other)
    {
        for(int i = 0; i < other.Count; i++) 
        {
            if (i < Count)
            {
                if (!Equals(this[i], other[i]))
                {
                    int oldIndex = IndexOf(other[i]);
                    if (oldIndex != -1)
                    {
                        MoveItem(oldIndex, i);
                    }
                    else if (other.Contains(this[i]))
                    {
                        InsertItem(i, other[i]);
                    }
                    else
                    {
                        SetItem(i, other[i]);
                    }
                }
            }
            else 
            {
                Add(other[i]);
            }
        }
        if(Count > other.Count) 
        {
            for(int i = Count - 1; i >= other.Count; i--) 
            {
                RemoveItem(i);
            }
        }
    }
}
