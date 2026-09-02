using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public interface IMediaFlipViewItemModel : IViewModel
{
    IMediaFileInfo MediaFile { get; }

    bool IsSelected { get; set; }

    bool IsDiashowActive { get; set; }

    Task? PlaybackCompletedTask => null;

    Task InitializeAsync();
}
