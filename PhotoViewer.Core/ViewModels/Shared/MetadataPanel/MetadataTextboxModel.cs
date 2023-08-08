using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Services;
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

    private bool IsDirty => timer.IsEnabled || IsWriting;

    private readonly IMetadataService metadataService;

    private readonly IMetadataProperty metadataProperty;

    private readonly ITimer timer;

    internal MetadataTextboxModel(
        IMessenger messenger,
        IMetadataService metadataService,
        IDialogService dialogService,
        IBackgroundTaskService backgroundTaskService,
        IMetadataProperty<string> metadataProperty,
        TimerFactory? timerFactory = null) : base(messenger, backgroundTaskService, dialogService)
    {
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        timer = (timerFactory ?? Timer.Create).Invoke(interval: WaitTime, autoRestart: false);
        timer.Elapsed += async (_, _) => await WriteValueAsync();
    }

    internal MetadataTextboxModel(
          IMessenger messenger,
        IMetadataService metadataService,
        IDialogService dialogService,
        IBackgroundTaskService backgroundTaskService,
        IMetadataProperty<string[]> metadataProperty,
        TimerFactory? timerFactory = null) : base(messenger, backgroundTaskService, dialogService)
    {
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        timer = (timerFactory ?? Timer.Create).Invoke(interval: WaitTime, autoRestart: false);
        timer.Elapsed += async (_, _) => await WriteValueAsync();
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
        if (metadataProperty == this.metadataProperty && !IsDirty)
        {
            Update(metadata);
        }
    }

    public void Update(IList<MetadataView> metadata)
    {
        IList<string> values;

        if (metadataProperty is IMetadataProperty<string> stringProperty)
        {
            values = metadata.Select(m => m.Get(stringProperty).Trim()).ToList();
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
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ConfirmAsync()
    {
        Log.Debug("ConfirmAsync invoked");
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
        var result = await WriteFilesAsync(async file =>
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
        }, cancelPrevious: true);

        if (!result.IsCanceld)
        {
            Messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, metadataProperty));
        }
    }
}
