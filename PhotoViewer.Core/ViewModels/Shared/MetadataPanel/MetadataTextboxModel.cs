using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;

namespace PhotoViewer.Core.ViewModels;

public partial class MetadataTextboxModel : MetadataPanelSectionModelBase
{
    public static readonly TimeSpan DebounceTime = TimeSpan.FromMilliseconds(500);

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

    private bool dirty = false;

    private readonly IMetadataService metadataService;

    private readonly IMetadataProperty metadataProperty;

    private readonly Debouncer<string> writeDebouncer;

    internal MetadataTextboxModel(
        IMessenger messenger,
        IMetadataService metadataService,
        IDialogService dialogService,
        IBackgroundTaskService backgroundTaskService,
        IMetadataProperty<string> metadataProperty,
        TimeProvider timeProvider)
        : base(messenger, backgroundTaskService, dialogService)
    {
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        writeDebouncer = new Debouncer<string>(DebounceTime, WriteFilesAsync, true, timeProvider);
    }

    internal MetadataTextboxModel(
        IMessenger messenger,
        IMetadataService metadataService,
        IDialogService dialogService,
        IBackgroundTaskService backgroundTaskService,
        IMetadataProperty<string[]> metadataProperty,
        TimeProvider timeProvider)
        : base(messenger, backgroundTaskService, dialogService)
    {
        this.metadataService = metadataService;
        this.metadataProperty = metadataProperty;
        writeDebouncer = new Debouncer<string>(DebounceTime, WriteFilesAsync, true, timeProvider);
    }

    protected override void OnCleanup()
    {
        writeDebouncer.Flush();
        writeDebouncer.Dispose();
    }

    protected override void BeforeFilesChanged()
    {
        writeDebouncer.Flush();
    }

    protected override void OnFilesChanged(IReadOnlyList<MetadataView> metadata)
    {
        Update(metadata);
    }

    protected override void OnMetadataModified(IReadOnlyList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        if (metadataProperty == this.metadataProperty && !dirty)
        {
            Update(metadata);
        }
    }

    public void Update(IReadOnlyList<MetadataView> metadata)
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
        await writeDebouncer.Flush();
    }

    private void OnTextChangedExternal()
    {
        writeDebouncer.Invoke(Text);
        HasMultipleValues = false;
    }

    private async Task WriteFilesAsync(string value)
    {
        await WriteFilesAndCancelPreviousAsync(async (file, cancellationToken) =>
        {
            if (metadataProperty is IMetadataProperty<string> stringProperty)
            {
                if (!Equals(value, await metadataService.GetMetadataAsync(file, stringProperty)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await metadataService.WriteMetadataAsync(file, stringProperty, value);
                }
            }
            else if (metadataProperty is IMetadataProperty<string[]> stringArrayProperty)
            {
                string[] arrayValue = value.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
                if (!Equals(arrayValue, await metadataService.GetMetadataAsync(file, stringArrayProperty)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await metadataService.WriteMetadataAsync(file, stringArrayProperty, arrayValue);
                }
            }
            else
            {
                throw new Exception("metadataProperty is invalid");
            }
        },
        (processedFiles) =>
        {
            Messenger.Send(new MetadataModifiedMessage(processedFiles, metadataProperty));
        });
    }
}
