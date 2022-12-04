using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerCore.Messages;
using PostSharp.Patterns.Model;

namespace PhotoViewerApp.Utils;

[NotifyPropertyChanged(ExcludeExplicitProperties = true)]
public class ViewModelBase : ObservableObject
{
    protected virtual void __EnableAutoNotifyCanExecuteChanged() { }

    protected virtual void __EnableOnPropertyChangedMethods() { }

    protected virtual void __EnableDependsOn() { }

    private readonly SynchronizationContext synchronizationContext;

    public ViewModelBase()
    {
        synchronizationContext = SynchronizationContext.Current!;
        __EnableAutoNotifyCanExecuteChanged();
        __EnableOnPropertyChangedMethods();
        __EnableDependsOn();
    }

    public virtual void OnViewConnected() { }

    public virtual void OnViewDisconnected() { }

    protected void RunOnUIThread(Action action)
    {
        synchronizationContext.Send(_ => action(), null);
    }
}