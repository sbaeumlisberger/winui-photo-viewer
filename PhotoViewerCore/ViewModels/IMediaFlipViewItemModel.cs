using PhotoViewerApp.Models;
using System.ComponentModel;

namespace PhotoViewerCore.ViewModels;

public interface IMediaFlipViewItemModel : INotifyPropertyChanged
{
    IMediaFileInfo MediaItem { get; }

    bool IsActive { get; set; }

    bool IsDiashowActive { get; set; }

    Task? PlaybackCompletedTask => null;

    Task PrepareAsync();

    void Cleanup();
}
