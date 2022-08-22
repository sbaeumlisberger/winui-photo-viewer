using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using System;
using System.ComponentModel;

namespace PhotoViewerApp.ViewModels;

public interface IMediaFlipViewItemModel : INotifyPropertyChanged
{
    IMediaItem MediaItem { get; }
}

public class MediaFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public event EventHandler<EventArgs>? Cleanup; // TODO?

    public IMediaItem MediaItem { get; }

    public MediaFlipViewItemModel(IMediaItem mediaItem)
    {
        MediaItem = mediaItem;
    }
}
