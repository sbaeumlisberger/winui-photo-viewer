using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.ViewModels;

public partial class VideoFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsActive { get; set; }

    public string WebViewContent { get; set; } = string.Empty;

    public VideoFlipViewItemModel(IMediaFileInfo mediaFile)
    {
        MediaItem = mediaFile;
    }

    public Task InitializeAsync()
    {
        WebViewContent = $"<html><body style=\"margin: 0px; background: black\"><video style=\"width: 100%; height: 100%\" controls><source src=\"http://files/{MediaItem.FileName}\" type=\"{MediaItem.StorageFile.ContentType}\"></video></body></html>";
        return Task.CompletedTask;
    }

    public void Cleanup()
    {
        WebViewContent = string.Empty;
    }
}
