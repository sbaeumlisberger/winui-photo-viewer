using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Utils;
using Tocronx.SimpleAsync;
using Windows.Foundation.Collections;

namespace PhotoViewerCore.ViewModels;

public partial class MetadataTextboxModel : MetadataPanelSectionModelBase
{
    private static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(1);

    public string Text
    {
        get => text;
        set
        {
            if (SetProperty(ref text, value))
            {
                OnTextChangedExternal();
            }
        }
    }
    private string text = string.Empty;

    public bool HasMultipleValues { get; private set; }

    public bool IsWriting { get; private set; }

    private readonly IMetadataService metadataService;

    private readonly IMetadataProperty metadataProperty;

    private readonly System.Timers.Timer timer = new System.Timers.Timer(WaitTime.TotalMilliseconds) { AutoReset = false };

    public MetadataTextboxModel(
        SequentialTaskRunner writeFilesRunner, 
        IMetadataService metadataService, 
        IMetadataProperty<string> metadataProperty) : base(writeFilesRunner, null!)
    {
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        timer.Elapsed += (_, _) => _ = WriteValueAsync();
    }

    public MetadataTextboxModel(
        SequentialTaskRunner writeFilesRunner, 
        IMetadataService metadataService, 
        IMetadataProperty<string[]> metadataProperty) : base(writeFilesRunner, null!)
    {
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        timer.Elapsed += (_, _) => _ = WriteValueAsync();
    }

    protected override void OnFilesChanged(IList<MetadataView> metadata)
    {
        Update(metadata);
    }

    protected override void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        if (metadataProperty == this.metadataProperty)
        {
            Update(metadata);
        }
    }

    public void Update(IList<MetadataView> metadata)
    {
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

        _ = EnsureValueWrittenAsync();

        if (values.Any())
        {
            HasMultipleValues = values.Any(v => !Equals(v, values.First()));
            text = HasMultipleValues ? string.Empty : values.First();
            OnPropertyChanged(nameof(Text));
        }
        else
        { 
            HasMultipleValues = false;
            text = string.Empty;
            OnPropertyChanged(nameof(Text));
        }
    }

    /// <summary>
    /// e.g. enter key pressed
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

    private void OnTextChangedExternal()
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
            await WriteFilesAsync(Text);
        }
    }

    private async Task WriteValueAsync()
    {
        RunOnUIThread(() => IsWriting = true);
        await WriteFilesAsync(Text).ConfigureAwait(false);
        RunOnUIThread(() => IsWriting = false);
    }

    private async Task WriteFilesAsync(string value)
    {
        await EnqueueWriteFiles(async (files) =>
        {
            var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
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
            });

            if(result.HasFailures)
            {
                // TODO show error
            }
        });
    }
}
