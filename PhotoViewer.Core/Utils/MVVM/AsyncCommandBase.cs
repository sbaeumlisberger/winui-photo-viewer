using System.Windows.Input;

namespace PhotoViewer.Core.Utils;

public interface IAsyncCommand
{
    Task ExecuteAsync();
}

public abstract partial class AsyncCommandBase : IAsyncCommand, ICommand
{
    public event EventHandler? CanExecuteChanged;

    protected virtual bool CanExecute() => true;

    private bool isExecuting = false;

    public bool CanExecute(object? parameter)
    {
        return !isExecuting && CanExecute();
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync()
    {
        isExecuting = true;
        RaiseCanExecuteChanged();
        await OnExecuteAsync();
        isExecuting = false;
        if (CanExecute(null))
        {
            RaiseCanExecuteChanged();
        }
    }

    protected void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    protected abstract Task OnExecuteAsync();
}