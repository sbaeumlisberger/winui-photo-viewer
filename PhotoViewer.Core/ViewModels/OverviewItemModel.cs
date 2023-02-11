using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels;

public partial class OverviewItemModel : ViewModelBase
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
        DisplayName = mediaFile.Name;
        this.metadataService = metadataService;
    }

    protected async override void OnViewConnectedOverride()
    {
        Messenger.Register<ChangeThumbnailSizeMessage>(this, Receive);
        Messenger.Register<BitmapRotatedMesssage>(this, Receive);

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

    private void Receive(BitmapRotatedMesssage msg)
    {
        if (msg.Bitmap == MediaFile)
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
