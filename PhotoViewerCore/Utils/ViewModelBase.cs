using CommunityToolkit.Mvvm.ComponentModel;
using PostSharp.Patterns.Model;

namespace PhotoViewerApp.Utils;

[NotifyPropertyChanged(ExcludeExplicitProperties = true)]
public class ViewModelBase : ObservableObject
{
    protected virtual void __EnableAutoNotifyCanExecuteChanged() { }

    protected virtual void __EnableCallOnPropertyChangedMethod() { }

    private readonly SynchronizationContext synchronizationContext;

    public ViewModelBase()
    {
        synchronizationContext = SynchronizationContext.Current!;
        __EnableAutoNotifyCanExecuteChanged();
        __EnableCallOnPropertyChangedMethod();
    }

    protected void RunOnUIThread(Action action)
    {
        synchronizationContext.Post(_ => action(), null);
    }
}