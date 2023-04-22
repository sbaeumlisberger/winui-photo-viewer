using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace PhotoViewer.App.Utils;

public interface IViewModel 
{
    void Cleanup();
}

public class ViewModelBase : ObservableObject, IViewModel
{
    public Task LastDispatchTask { get; private set; } = Task.CompletedTask;

    protected IMessenger Messenger { get; }

    private readonly SynchronizationContext? synchronizationContext;

    private readonly bool lifecyleMangedByView; // TODO remove

    public ViewModelBase(IMessenger messenger = null!, bool lifecyleMangedByView = true)
    {
        Messenger = messenger;
        synchronizationContext = SynchronizationContext.Current;
        this.lifecyleMangedByView = lifecyleMangedByView;
        __EnableAutoNotifyCanExecuteChanged();
        __EnableOnPropertyChangedMethods();
    }

    public void Cleanup() 
    {
        Messenger?.UnregisterAll(this);
        OnCleanup();
    }

    public void OnViewConnected()
    {
        if (lifecyleMangedByView)
        {
            Messenger?.RegisterAll(this);
            OnViewConnectedOverride();
        }
    }

    public void OnViewDisconnected()
    {
        if (lifecyleMangedByView)
        {
            Cleanup();
            OnViewDisconnectedOverride();
        }
    }

    protected virtual void OnCleanup() { }

    protected virtual void OnViewConnectedOverride() { }

    protected virtual void OnViewDisconnectedOverride() { }

    protected Task RunOnUIThreadAsync(Action action)
    {
        if (synchronizationContext is null)
        {
            throw new InvalidOperationException();
        }

        if (SynchronizationContext.Current != synchronizationContext)
        {
            var tsc = new TaskCompletionSource();
            LastDispatchTask = tsc.Task;
            synchronizationContext.Post(_ => { action(); tsc.SetResult(); }, null);
            return tsc.Task;
        }
        else
        {
            action();
            return Task.CompletedTask;
        }
    }

    protected virtual void __EnableAutoNotifyCanExecuteChanged() { }

    protected virtual void __EnableOnPropertyChangedMethods() { }
}