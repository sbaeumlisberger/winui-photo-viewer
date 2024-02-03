using Essentials.NET;
using System;

namespace PhotoViewer.Core.Utils;

public partial class Progress : ObservableObjectBase, IProgress<double>
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

    private readonly object lockObject = new object();

    private readonly Throttle throttle;

    private double value;
    private TimeSpan? estimatedTimeRemaining;

    public Progress(CancellationTokenSource? cts = null, SynchronizationContext? synchronizationContext = null, TimeProvider? timeProvider = null)
    {
        this.cts = cts;
        this.synchronizationContext = synchronizationContext ?? SynchronizationContext.Current!;
        throttle = new Throttle(TimeSpan.FromMilliseconds(40), timeProvider);
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

        lock (lockObject)
        {
            if (progress < value)
            {
                return;
            }

            this.value = progress;
            this.estimatedTimeRemaining = estimatedTimeRemaining ?? EstimateTimeRemaining(progress);

            throttle.Invoke(UpdateAsync);
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

    private async Task UpdateAsync()
    {
        await synchronizationContext.DispatchAsync(() =>
        {
            Value = value;
            EstimatedTimeRemaining = estimatedTimeRemaining;

            if (Value == 1.0)
            {
                IsCompleted = true;
                IsActive = false;
                CanCancel = false;
            }
        });
    }

    private TimeSpan? EstimateTimeRemaining(double progress)
    {
        if (progress > 0)
        {
            return (DateTime.Now - startTime) / progress * (1 - progress);
        }
        return null;
    }
}
