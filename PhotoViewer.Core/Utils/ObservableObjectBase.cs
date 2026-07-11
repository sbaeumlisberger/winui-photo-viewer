using CommunityToolkit.Mvvm.ComponentModel;
using Essentials.NET;
using System.ComponentModel;

namespace PhotoViewer.Core.Utils;

public partial class ObservableObjectBase : ObservableObject
{
    public record class Subscription
    {
        public object Subscriber { get; }
        public string PropertyName { get; }
        public Action Callback { get; }

        public Subscription(object subscriber, string propertyName, Action callback)
        {
            Subscriber = subscriber;
            PropertyName = propertyName;
            Callback = callback;
        }
    }

    private readonly List<Subscription> subscriptions = new List<Subscription>();

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        subscriptions
            .Where(subscription => subscription.PropertyName == e.PropertyName)
            .ForEach(subscription => subscription.Callback());

        _NotifyComputedPropertyChanged(e.PropertyName);
    }

    protected virtual void _NotifyComputedPropertyChanged(string? propertyName) { }

    public Subscription Subscribe(object subscriber, string propertyName, Action callback, bool initialCallback = false)
    {
        var subscription = new Subscription(subscriber, propertyName, callback);
        subscriptions.Add(subscription);
        if (initialCallback)
        {
            callback();
        }
        return subscription;
    }

    public void Unsubscribe(Subscription subscription)
    {
        subscriptions.Remove(subscription);
    }

    public void UnsubscribeAll(object subscriber)
    {
        subscriptions.RemoveAll(subscription => subscription.Subscriber == subscriber);
    }
}
