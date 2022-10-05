using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;

namespace PhotoViewerCore.ViewModels;

public partial class MetadataTextboxModel : ViewModelBase
{
    private static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(1);

    public string Value
    {
        get => _Value;
        set
        {
            if (SetProperty(ref _Value, value))
            {
                OnValueChangedExternal();
            }
        }
    }
    private string _Value = string.Empty;

    public bool HasMultipleValues { get; private set; }

    public bool IsWriting { get; private set; }

    private readonly Func<string, Task> writeFunction;

    private readonly System.Timers.Timer timer = new System.Timers.Timer(WaitTime.TotalMilliseconds) { AutoReset = false };

    public MetadataTextboxModel(Func<string, Task> writeFunction)
    {
        this.writeFunction = writeFunction;
        timer.Elapsed += (_, _) => _ = WriteValueAsync();
    }

    public void SetValues(IList<string> values)
    {
        if (values.Any())
        {
            EnsureValueWritten();
            HasMultipleValues = values.Any(v => !Equals(v, values.First()));
            _Value = HasMultipleValues ? string.Empty : values.First();
            OnPropertyChanged(nameof(Value));
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        EnsureValueWritten();
        HasMultipleValues = false;
        _Value = string.Empty;
        OnPropertyChanged(nameof(Value));
    }

    /// <summary>
    /// e.g. Enter key pressed
    /// </summary>
    [RelayCommand]
    private async void SignalTypingCompleted()
    {
        Log.Debug("SignalTypingCompleted invoked");
        if (timer.Enabled)
        {
            timer.Stop();
            await WriteValueAsync();
        }
    }

    private void OnValueChangedExternal()
    {
        timer.Stop();
        timer.Start();
        HasMultipleValues = false;
    }

    private async void EnsureValueWritten()
    {
        if (timer.Enabled)
        {
            timer.Stop();
            await writeFunction.Invoke(Value);
        }
    }

    private async Task WriteValueAsync()
    {
        RunOnUIThread(() => IsWriting = true);
        await writeFunction.Invoke(Value); // TODO handle erros
        RunOnUIThread(() => IsWriting = false);
    }

}
