using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using PhotoViewerApp.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace PhotoViewerApp.Views;

public sealed partial class VideoFlipViewItem : UserControl
{
    private VideoFlipViewItemModel? ViewModel { get; set; }

    public VideoFlipViewItem()
    {
        this.InitializeComponent();
        DataContextChanged += VideoFlipViewItem_DataContextChanged;
    }

    private async void VideoFlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        ViewModel = (VideoFlipViewItemModel)DataContext;

        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (ViewModel.WebViewContent is string videoTag)
            {
                await ShowVideoAsync(videoTag);
            }
        }
    }

    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.WebViewContent))
        {
            if (ViewModel!.WebViewContent is string videoTag)
            {
                await ShowVideoAsync(videoTag);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsActive))
        {
            if (ViewModel!.IsActive is false)
            {
                await PauseVideoAsync();
            }
        }
    }

    private async Task ShowVideoAsync(string videoTag)
    {
        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping("files", Path.GetDirectoryName(ViewModel!.MediaItem.FilePath), CoreWebView2HostResourceAccessKind.Allow);
        webView.NavigateToString(videoTag);
    }

    private async Task PauseVideoAsync()
    {
        await webView.EnsureCoreWebView2Async();
        await webView.ExecuteScriptAsync("var video = document.getElementsByTagName('video')[0]; video.pause(); video.currentTime = 0;");
    }
}
