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
using Tocronx.SimpleAsync;

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

    private double progressPrivate;
    private bool updateDispatched = false;

    public Progress(CancellationTokenSource? cts = null, SynchronizationContext? synchronizationContext = null)
    {
        this.cts = cts;
        this.synchronizationContext = synchronizationContext ?? SynchronizationContext.Current!;
        CanCancel = cts != null;
    }

    public void Report(double progress)
    {
        Report(progress, null);     
    }

    public void Report(double progress, TimeSpan? estimatedTimeRemaining = null)
    {
        if (progress < 0 || progress > 1) 
        {
            throw new ArgumentOutOfRangeException(nameof(progress));
        }

        lock (this)
        {
            progressPrivate = progress;

            if (updateDispatched) 
            {
                return;
            }

            updateDispatched = true;
          
            synchronizationContext.Post(_ =>
            {
                updateDispatched = false;
                UpdateProgress(progressPrivate);
                EstimatedTimeRemaining = estimatedTimeRemaining ?? EstimateTimeRemaining();
            }, null);
        }
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
