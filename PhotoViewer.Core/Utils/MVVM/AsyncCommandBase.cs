﻿using System.Collections;
using System.Windows.Input;

namespace PhotoViewer.Core.Utils;

public abstract class AsyncCommandBase : ICommand
{
    public event EventHandler? CanExecuteChanged;

    private bool isExecuting = false;

    public bool CanExecute(object? parameter)
    {
        return !isExecuting && CanExecute();
    }

    public async void Execute(object? parameter)
    {
        isExecuting = true;
        RaiseCanExecuteChanged();
        await ExecuteAsync();
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

    protected virtual bool CanExecute() => true;

    protected abstract Task ExecuteAsync();
}

public abstract class AsyncCommandBase<T> : ICommand
{
    public event EventHandler? CanExecuteChanged;

    private bool isExecuting  = false;

    public bool CanExecute(object? parameter)
    {
        return parameter != null && !isExecuting && CanExecute((T)parameter);
    }

    public async void Execute(object? parameter)
    {
        isExecuting = true;
        RaiseCanExecuteChanged();
        await ExecuteAsync((T)parameter!);
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

    protected virtual bool CanExecute(T parameter) => true;

    protected abstract Task ExecuteAsync(T parameter);
}
