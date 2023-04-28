using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using Tocronx.SimpleAsync;

namespace PhotoViewer.Core.ViewModels;

public interface IOverviewItemModel { }

public partial class OverviewItemModel : ViewModelBase, IOverviewItemModel
{
    public event EventHandler<EventArgs>? ThumbnailInvalidated;

    public IMediaFileInfo MediaFile { get; }

    public string DisplayName { get; }

    public double ThumbnailSize { get; private set; } = 160;

    public bool HasKeywords { get; private set; } = false;

    public bool HasPeopleTags { get; private set; } = false;

    public bool HasLocation { get; private set; } = false;

    public int Rating { get; private set; } = 0;

    private readonly IMetadataService metadataService;

    public OverviewItemModel(IMediaFileInfo mediaFile, IMessenger messenger, IMetadataService metadataService) : base(messenger)
    {
        MediaFile = mediaFile;
        DisplayName = mediaFile.DisplayName;
        this.metadataService = metadataService;
        InitializeAsync().FireAndForget();
    }

    private async Task InitializeAsync()
    {
        Messenger.Register<ChangeThumbnailSizeMessage>(this, Receive);
        Messenger.Register<BitmapModifiedMesssage>(this, Receive);

        if (MediaFile is IBitmapFileInfo bitmapFile && bitmapFile.IsMetadataSupported)
        {
            Messenger.Register<MetadataModifiedMessage>(this, Receive);
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

    private async void Receive(MetadataModifiedMessage msg)
    {
        if (msg.Files.Contains(MediaFile))
        {
            await LoadMetadataInfoAsync((IBitmapFileInfo)MediaFile);
        }
    }

    private async Task LoadMetadataInfoAsync(IBitmapFileInfo bitmapFile)
    {
        var metadata = await metadataService.GetMetadataAsync(bitmapFile);
        HasKeywords = metadata.Get(MetadataProperties.Keywords).Any();
        HasPeopleTags = metadata.Get(MetadataProperties.People).Any();
        HasLocation = metadata.Get(MetadataProperties.GeoTag) != null || metadata.Get(MetadataProperties.Address) != null;
        Rating = metadata.Get(MetadataProperties.Rating);
    }

}
