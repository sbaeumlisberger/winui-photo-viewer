using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PhotoViewerCore.Utils;

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
}
