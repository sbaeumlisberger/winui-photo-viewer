using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.App.Utils;

public class ViewModelBase : ObservableObject
{
    protected virtual void __EnableAutoNotifyCanExecuteChanged() { }

    protected virtual void __EnableOnPropertyChangedMethods() { }

    protected IMessenger Messenger { get; }

    private readonly SynchronizationContext synchronizationContext;

    public ViewModelBase(IMessenger messenger = null!)
    {
        Messenger = messenger;
        synchronizationContext = SynchronizationContext.Current!;
        __EnableAutoNotifyCanExecuteChanged();
        __EnableOnPropertyChangedMethods();
    }

    public void OnViewConnected() 
    {
        Messenger?.RegisterAll(this);
        OnViewConnectedOverride();
    }

    protected virtual void OnViewConnectedOverride() { }

    public void OnViewDisconnected() 
    {
        Messenger?.UnregisterAll(this);
        OnViewDisconnectedOverride();
    }

    protected virtual void OnViewDisconnectedOverride() { }

    protected void RunOnUIThread(Action action)
    {
        if (SynchronizationContext.Current != synchronizationContext)
        {
            synchronizationContext.Post(_ => action(), null);
        }
    }
}