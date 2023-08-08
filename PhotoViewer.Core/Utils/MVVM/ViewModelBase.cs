using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using PropertyChanged;
using System.ComponentModel;

namespace PhotoViewer.App.Utils;

public interface IViewModel : INotifyPropertyChanged 
{
    void Cleanup();
}

public class ViewModelBase : ObservableObjectBase, IViewModel
{
    [DoNotNotify]
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
        _NotifyCanExecuteChanged(e.PropertyName);
        _InvokeOnPropertyChangedMethod(e.PropertyName);

        base.OnPropertyChanged(e);
    }

    protected virtual void _NotifyCanExecuteChanged(string? propertyName) { }

    protected virtual void _InvokeOnPropertyChangedMethod(string? propertyName) { }

    protected void Register<TMessage>(Action<TMessage> messageHandler) where TMessage : class
    {
        Messenger.Register<TMessage>(this, (_, msg) => RunInContextAsync(() => messageHandler(msg)));
    }

    /// <summary>
    /// Runs code in the view models synchronization context.
    /// </summary>
    protected Task RunInContextAsync(Action action)
    {
        if (synchronizationContext is null)
        {
            throw new InvalidOperationException();
        }

        if (SynchronizationContext.Current != synchronizationContext)
        {
            var tsc = new TaskCompletionSource();
            try
            {             
                LastDispatchTask = tsc.Task;
                synchronizationContext.Post(_ =>
                {
                    try
                    {
                        action();
                        tsc.SetResult();
                    }
                    catch (Exception e)
                    {
                        tsc.SetException(e);
                    }
                }, null);             
            }
            catch (Exception e)
            {
                tsc.SetException(e);
            }
            return tsc.Task;
        }
        else
        {
            action();
            return Task.CompletedTask;
        }
    }

}