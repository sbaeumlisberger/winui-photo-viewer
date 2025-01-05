using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using PhotoViewer.Core.Utils;
using System.ComponentModel;

namespace PhotoViewer.Core.Utils;

public interface IViewModel : INotifyPropertyChanged
{
    void Cleanup();
}

public class ViewModelBase : ObservableObjectBase, IViewModel
{
    internal Task LastDispatchTask { get; private set; } = Task.CompletedTask;

    protected IMessenger Messenger { get; }

    private readonly SynchronizationContext? synchronizationContext;

    public ViewModelBase(IMessenger messenger = null!)
    {
        Messenger = messenger;
        synchronizationContext = SynchronizationContext.Current;
    }

    public void Cleanup()
    {
        //Log.Debug($"Cleanup {this}");
        Messenger?.UnregisterAll(this);
        OnCleanup();
    }

    protected virtual void OnCleanup() { }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        _NotifyCanExecuteChanged(e.PropertyName);
    }

    protected virtual void _NotifyCanExecuteChanged(string? propertyName) { }

    protected void Register<TMessage>(Action<TMessage> messageHandler) where TMessage : class
    {
        Messenger.Register<TMessage>(this, (_, msg) => DispatchAsync(() => messageHandler(msg)));
    }

    /// <summary>
    /// Runs the specified action in the view models synchronization context.
    /// </summary>
    protected Task DispatchAsync(Action action)
    {
        LastDispatchTask = synchronizationContext.DispatchAsync(action);
        return LastDispatchTask;
    }

}