using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using System.ComponentModel;

namespace PhotoViewer.Core.ViewModels;

public interface IMediaFlipViewItemModel : IViewModel
{
    IMediaFileInfo MediaItem { get; }

    bool IsSelected { get; set; }

    bool IsDiashowActive { get; set; }

    Task? PlaybackCompletedTask => null;

    Task InitializeAsync();
}
