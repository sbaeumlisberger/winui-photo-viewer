using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerApp.ViewModels;

public partial class VideoFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }


    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private string webViewContent = string.Empty;

    public VideoFlipViewItemModel(IMediaFileInfo mediaFile)
    {
        MediaItem = mediaFile;    
    }

    public void StartLoading()
    {
        WebViewContent = $"<html><body style=\"margin: 0px; background: black\"><video style=\"width: 100%; height: 100%\" controls><source src=\"http://files/{MediaItem.File.Name}\" type=\"{MediaItem.File.ContentType}\"></video></body></html>";
    }

    public void Cleanup()
    {
        WebViewContent = string.Empty;
    }
}
