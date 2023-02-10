using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    public Progress(CancellationTokenSource? cts = null)
    {
        this.cts = cts;
        CanCancel = cts != null;
    }

    public void Report(double progress)
    {
        UpdateProgress(progress);
        EstimatedTimeRemaining = EstimateTimeRemaining();
    }

    public void Report(double progress, TimeSpan estimatedTimeRemaining)
    {
        UpdateProgress(progress);
        EstimatedTimeRemaining = estimatedTimeRemaining;
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

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        cts!.Cancel();
    }

    private TimeSpan EstimateTimeRemaining()
    {
        return (DateTime.Now - startTime) / Value * (1 - Value);
    }
}
