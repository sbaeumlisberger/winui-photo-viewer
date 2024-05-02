using Essentials.NET;

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

    private readonly Throttle updateThrottle;

    private readonly DateTime startTime = DateTime.Now;

    private readonly object lockObject = new object();

    private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;

    private double value;

    private TimeSpan? estimatedTimeRemaining;

    public Progress(CancellationTokenSource? cts = null, TimeProvider? timeProvider = null)
    {
        this.cts = cts;
        updateThrottle = new Throttle(TimeSpan.FromMilliseconds(30), UpdateAsync, true, timeProvider);
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

            updateThrottle.Invoke();
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
