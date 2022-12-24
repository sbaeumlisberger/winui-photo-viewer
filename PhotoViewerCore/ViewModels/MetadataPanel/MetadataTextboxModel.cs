using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using Tocronx.SimpleAsync;
using Windows.Foundation.Collections;

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

    private readonly SequentialTaskRunner writeFilesRunner;

    private readonly IMetadataService metadataService;

    private readonly IMetadataProperty metadataProperty;

    private readonly System.Timers.Timer timer = new System.Timers.Timer(WaitTime.TotalMilliseconds) { AutoReset = false };

    private IReadOnlyList<IBitmapFileInfo> files = Array.Empty<IBitmapFileInfo>();

    public MetadataTextboxModel(SequentialTaskRunner writeFilesRunner, IMetadataService metadataService, IMetadataProperty<string> metadataProperty)
    {
        this.writeFilesRunner = writeFilesRunner;
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        timer.Elapsed += (_, _) => _ = WriteValueAsync();
    }

    public MetadataTextboxModel(SequentialTaskRunner writeFilesRunner, IMetadataService metadataService, IMetadataProperty<string[]> metadataProperty)
    {
        this.writeFilesRunner = writeFilesRunner;
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        timer.Elapsed += (_, _) => _ = WriteValueAsync();
    }

    public void Update(IReadOnlyList<IBitmapFileInfo> files, IList<MetadataView> metadata)
    {
        this.files = files;

        IList<string> values;

        if (metadataProperty is IMetadataProperty<string> stringProperty)
        {
            values = metadata.Select(m => m.Get(stringProperty)).ToList();
        }
        else if (metadataProperty is IMetadataProperty<string[]> stringArrayProperty)
        {
            values = metadata.Select(m => string.Join("; ", m.Get(stringArrayProperty))).ToList();
        }
        else 
        {
            throw new Exception("metadataProperty is invalid");
        }

        if (values.Any())
        {
            _ = EnsureValueWrittenAsync();
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
        _ = EnsureValueWrittenAsync();
        files = Array.Empty<IBitmapFileInfo>();
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

    private async Task EnsureValueWrittenAsync()
    {
        if (timer.Enabled)
        {
            timer.Stop();
            await WriteFilesAsync(Value, files.ToList());
        }
    }

    private async Task WriteValueAsync()
    {
        RunOnUIThread(() => IsWriting = true);
        try
        {
            await WriteFilesAsync(Value, files.ToList());
        }
        catch(Exception e) 
        {
            // TODO handle erros
            Log.Error("WriteFiles failed", e);
        }
        RunOnUIThread(() => IsWriting = false);
    }

    private async Task WriteFilesAsync(string value, IList<IBitmapFileInfo> files)
    {
        await writeFilesRunner.Enqueue(async () =>
        {
            foreach (var file in files)
            {
                if (metadataProperty is IMetadataProperty<string> stringProperty)
                {
                    if (!Equals(value, await metadataService.GetMetadataAsync(file, stringProperty)))
                    {
                        await metadataService.WriteMetadataAsync(file, stringProperty, value);
                    }
                }
                else if (metadataProperty is IMetadataProperty<string[]> stringArrayProperty)
                {
                    string[] arrayValue = value.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
                    if (!Equals(arrayValue, await metadataService.GetMetadataAsync(file, stringArrayProperty)))
                    {
                        await metadataService.WriteMetadataAsync(file, stringArrayProperty, arrayValue);
                    }
                }
                else
                {
                    throw new Exception("metadataProperty is invalid");
                }
            }
        });
        // TODO prallelize, handle errors
    }

}
