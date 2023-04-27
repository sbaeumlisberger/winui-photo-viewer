using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;

namespace PhotoViewer.App.Utils;

public interface IViewModel : INotifyPropertyChanged 
{
    void Initialize();
    void Cleanup();
}

public class ViewModelBase : ObservableObject, IViewModel
{
    public Task LastDispatchTask { get; private set; } = Task.CompletedTask;

    protected IMessenger Messenger { get; }

    private readonly SynchronizationContext? synchronizationContext;

    public ViewModelBase(IMessenger messenger = null!)
    {
        Messenger = messenger;
        synchronizationContext = SynchronizationContext.Current;
        __EnableAutoNotifyCanExecuteChanged();
        __EnableOnPropertyChangedMethods();
    }

    public void Initialize()
    {
        Messenger?.RegisterAll(this);
        OnInitialize();
    }

    public void Cleanup()
    {
        Messenger?.UnregisterAll(this);
        OnCleanup();
    }

    protected virtual void OnInitialize() { }

    protected virtual void OnCleanup() { }

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