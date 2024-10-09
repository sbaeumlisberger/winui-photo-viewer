using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using MetadataAPI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public interface IOverviewItemModel : IViewModel { }

public partial class OverviewItemModel : ViewModelBase, IOverviewItemModel
{
    public event EventHandler<EventArgs>? ThumbnailInvalidated;

    public IMediaFileInfo MediaFile { get; }

    public string DisplayName { get; private set; }

    public string NewName { get; set; }

    public double ThumbnailSize { get; private set; } = 160;

    public bool HasKeywords { get; private set; } = false;

    public bool HasPeopleTags { get; private set; } = false;

    public bool HasLocation { get; private set; } = false;

    public int Rating { get; private set; } = 0;

    public bool IsRenaming { get; private set; } = false;

    public bool IsNotRenaming => !IsRenaming;

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    internal OverviewItemModel(IMediaFileInfo mediaFile, IMessenger messenger, IMetadataService metadataService, IDialogService dialogService) : base(messenger)
    {
        MediaFile = mediaFile;
        DisplayName = mediaFile.DisplayName;
        NewName = mediaFile.FileNameWithoutExtension;
        this.metadataService = metadataService;
        this.dialogService = dialogService;
        InitializeAsync().LogOnException();
    }

    private async Task InitializeAsync()
    {
        Register<ChangeThumbnailSizeMessage>(Receive);
        Register<BitmapModifiedMesssage>(Receive);
        Register<ActivateRenameFileMessage>(Receive);

        if (MediaFile is IBitmapFileInfo bitmapFile && bitmapFile.IsMetadataSupported)
        {
            Register<MetadataModifiedMessage>(Receive);
            await LoadMetadataInfoAsync(bitmapFile);
        }
    }
    private void Receive(ChangeThumbnailSizeMessage msg)
    {
        ThumbnailSize = msg.NewThumbnailSize;
    }

    private void Receive(BitmapModifiedMesssage msg)
    {
        if (msg.BitmapFile == MediaFile)
        {
            ThumbnailInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Receive(ActivateRenameFileMessage msg)
    {
        if (msg.MediaFile == MediaFile)
        {
            IsRenaming = true;
        }
    }

    private async void Receive(MetadataModifiedMessage msg)
    {
        if (msg.Files.Contains(MediaFile))
        {
            await LoadMetadataInfoAsync((IBitmapFileInfo)MediaFile);
        }
    }

    private async Task LoadMetadataInfoAsync(IBitmapFileInfo bitmapFile)
    {
        try
        {
            var metadata = await metadataService.GetMetadataAsync(bitmapFile);
            HasKeywords = metadata.Get(MetadataProperties.Keywords).Any();
            HasPeopleTags = metadata.Get(MetadataProperties.People).Any();
            HasLocation = metadata.Get(MetadataProperties.GeoTag) != null || metadata.Get(MetadataProperties.Address) != null;
            Rating = metadata.Get(MetadataProperties.Rating);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load metadata for {bitmapFile.FileName}", ex);
        }
    }

    public void CancelRenaming()
    {
        Log.Info("User canceld renaming");
        ExitRenaming();
    }

    public async Task ConfirmRenaming()
    {
        if (!IsRenaming) { throw new InvalidOperationException(); }

        if (string.IsNullOrWhiteSpace(NewName))
        {
            Log.Debug("User provided invalid file name");
            ExitRenaming();
            return;
        }

        if (NewName == MediaFile.FileNameWithoutExtension)
        {
            Log.Debug("Exit renaming without change");
            ExitRenaming();
            return;
        }

        Log.Info($"Rename {MediaFile} to {NewName}");

        try
        {
            await MediaFile.RenameAsync(NewName);
            DisplayName = MediaFile.DisplayName;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to rename {MediaFile} to {NewName}", ex);
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.RenameFileErrorDialog_Title,
                Message = ex.Message
            });
        }

        ExitRenaming();
    }

    private void ExitRenaming()
    {
        NewName = MediaFile.FileNameWithoutExtension;
        IsRenaming = false;
    }

    public override string ToString()
    {
        return nameof(OverviewItemModel) + ":" + DisplayName;
    }
}
