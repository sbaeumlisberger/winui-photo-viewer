using CommunityToolkit.Mvvm.ComponentModel;
using Essentials.NET;
using System.ComponentModel;

namespace PhotoViewer.Core.Utils;

public class ObservableObjectBase : ObservableObject
{
    public record class Subsciption
    {
        public object Subscriber { get; }
        public string PropertyName { get; }
        public Action Callback { get; }

        public Subsciption(object subscriber, string propertyName, Action callback)
        {
            Subscriber = subscriber;
            PropertyName = propertyName;
            Callback = callback;
        }
    }

    private readonly List<Subsciption> subscriptions = new List<Subsciption>();

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        subscriptions
            .Where(subscription => subscription.PropertyName == e.PropertyName)
            .ForEach(subscription => subscription.Callback());
    }

    public Subsciption Subscribe(object subscriber, string propertyName, Action callback, bool initialCallback = false)
    {
        var subsciption = new Subsciption(subscriber, propertyName, callback);
        subscriptions.Add(subsciption);
        if (initialCallback)
        {
            callback();
        }
        return subsciption;
    }

    public void Unsubscribe(Subsciption subsciption)
    {
        subscriptions.Remove(subsciption);
    }

    public void UnsubscribeAll(object subscriber)
    {
        subscriptions.RemoveAll(subscription => subscription.Subscriber == subscriber);
    }
}
