using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Services;
using System;

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

    private bool IsDirty => timer != null && IsWriting;

    private readonly IMetadataService metadataService;

    private readonly IMetadataProperty metadataProperty;

    private readonly TimeProvider timeProvider;

    private ITimer? timer;

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
        this.timeProvider = timeProvider;
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
        this.timeProvider = timeProvider;
    }

    protected override void OnCleanup()
    {
        timer.DisposeSafely(() => timer = null);
    }

    protected async override void BeforeFilesChanged()
    {
        timer.DisposeSafely(() => timer = null);
        await WriteFilesAsync(Text);
    }

    protected override void OnFilesChanged(IReadOnlyList<MetadataView> metadata)
    {
        Update(metadata);
    }

    protected override void OnMetadataModified(IReadOnlyList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        if (metadataProperty == this.metadataProperty && !IsDirty)
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
        timer.DisposeSafely(() => timer = null);
        await WriteValueAsync();        
    }

    private void OnTextChangedExternal()
    {
        timer.DisposeSafely(() => timer = null);
        timer = timeProvider.CreateTimer(async _ => await WriteValueAsync(), WaitTime);
        HasMultipleValues = false;
    }

    private async Task WriteValueAsync()
    {
        await WriteFilesAsync(Text).ConfigureAwait(false);
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
