using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

public partial class Progress : ObservableObject, IProgress<double>
{
    public bool CanCancel { get; private set; } = false;

    public double Value { get; private set; } = 0;

    public TimeSpan? EstimatedTimeRemaining { get; private set; }

    public bool IsActive { get; private set; } = true;

    public bool IsCompleted { get; private set; } = false;

    public bool IsFailed { get; private set; } = false;

    private readonly CancellationTokenSource? cts;

    private readonly DateTime startTime = DateTime.Now;

    private readonly SynchronizationContext synchronizationContext;

    public Progress(CancellationTokenSource? cts = null, SynchronizationContext? synchronizationContext = null)
    {
        this.cts = cts;
        this.synchronizationContext = synchronizationContext ?? SynchronizationContext.Current!;
        CanCancel = cts != null;
    }

    public void Report(double progress)
    {
        // TODO buffer
        synchronizationContext.Post(_ =>
        {
            UpdateProgress(progress);
            EstimatedTimeRemaining = EstimateTimeRemaining();
        }, null);
    }

    public void Report(double progress, TimeSpan estimatedTimeRemaining)
    { 
        // TODO buffer
        synchronizationContext.Post(_ =>
        {
            UpdateProgress(progress);
            EstimatedTimeRemaining = estimatedTimeRemaining;
        }, null);
    }

    private void UpdateProgress(double progress)
    {
        Value = progress;
        IsCompleted = Value == 1.0;
        if (IsCompleted)
        {
            IsActive = false;
            CanCancel = false;
        }
    }

    public void Fail()
    {
        IsFailed = true;
        IsActive = false;
        CanCancel = false;
    }

    [RelayCommand(CanExecute = nameof(CanCancel))] // TODO remove command
    public void Cancel()
    {
        cts?.Cancel();
    }

    private TimeSpan? EstimateTimeRemaining()
    {
        if (Value > 0)
        {
            return (DateTime.Now - startTime) / Value * (1 - Value);
        }
        return null;
    }
}
