using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using Tocronx.SimpleAsync;
using Timer = PhotoViewer.Core.Utils.Timer;

namespace PhotoViewer.Core.ViewModels;

public partial class MetadataTextboxModel : MetadataPanelSectionModelBase
{
    public static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(1);

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

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    private readonly IMetadataProperty metadataProperty;

    private readonly ITimer timer;

    internal MetadataTextboxModel(
        SequentialTaskRunner writeFilesRunner,
        IMetadataService metadataService,
        IDialogService dialogService,
        IMetadataProperty<string> metadataProperty,
        TimerFactory? timerFactory = null) : base(writeFilesRunner, null!)
    {
        this.metadataService = metadataService;
        this.dialogService = dialogService;
        this.metadataProperty = metadataProperty;
        timer = (timerFactory ?? Timer.Create).Invoke(interval: WaitTime, autoRestart: false);
        timer.Elapsed += (_, _) => _ = WriteValueAsync();
    }

    internal MetadataTextboxModel(
        SequentialTaskRunner writeFilesRunner,
        IMetadataService metadataService,
        IDialogService dialogService,
        IMetadataProperty<string[]> metadataProperty,
        TimerFactory? timerFactory = null) : base(writeFilesRunner, null!)
    {
        this.metadataService = metadataService;
        this.dialogService = dialogService;
        this.metadataProperty = metadataProperty;
        timer = (timerFactory ?? Timer.Create).Invoke(interval: WaitTime, autoRestart: false);
        timer.Elapsed += (_, _) => _ = WriteValueAsync();
    }

    protected override void OnCleanup()
    {
        timer.Dispose();
    }

    protected async override void BeforeFilesChanged()
    {
        if (timer.IsEnabled)
        {
            timer.Stop();
            await WriteFilesAsync(Text);
        }
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
        if (timer.IsEnabled)
        {
            timer.Stop();
            await WriteValueAsync();
        }
    }

    private void OnTextChangedExternal()
    {
        timer.Restart();
        HasMultipleValues = false;
    }

    private async Task WriteValueAsync()
    {
        await WriteFilesAsync(Text).ConfigureAwait(false);
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

            if (result.HasFailures)
            {
                await ShowWriteMetadataFailedDialog(dialogService, result);
            }
        });
    }
}
