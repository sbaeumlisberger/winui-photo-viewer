using PhotoViewer.App.Models;
using System.ComponentModel;

namespace PhotoViewer.Core.ViewModels;

public interface IMediaFlipViewItemModel : INotifyPropertyChanged
{
    IMediaFileInfo MediaItem { get; }

    bool IsSelected { get; set; }

    bool IsDiashowActive { get; set; }

    Task? PlaybackCompletedTask => null;

    Task InitializeAsync();

    void Cleanup();
}
