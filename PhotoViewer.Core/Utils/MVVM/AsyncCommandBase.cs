using System.Windows.Input;

namespace PhotoViewer.Core.Utils;

public interface IAsyncCommand
{
    Task ExecuteAsync();
}

public abstract class AsyncCommandBase : IAsyncCommand, ICommand
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

public interface IAsyncCommand<T>
{
    Task ExecuteAsync(T parameter);
}

public abstract class AsyncCommandBase<T> : IAsyncCommand<T>, ICommand
{
    public event EventHandler? CanExecuteChanged;

    protected virtual bool CanExecute(T parameter) => true;


    private bool isExecuting = false;

    public bool CanExecute(object? parameter)
    {
        return parameter != null && !isExecuting && CanExecute((T)parameter);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync((T)parameter!);
    }

    public async Task ExecuteAsync(T parameter)
    {
        isExecuting = true;
        RaiseCanExecuteChanged();
        await OnExecuteAsync(parameter);
        isExecuting = false;
        if (CanExecute(parameter))
        {
            RaiseCanExecuteChanged();
        }
    }

    protected void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    protected abstract Task OnExecuteAsync(T parameter);
}
