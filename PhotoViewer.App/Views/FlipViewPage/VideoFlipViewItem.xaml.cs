using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using PhotoVieweApp.Utils;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace PhotoViewer.App.Views;

public sealed partial class VideoFlipViewItem : UserControl, IMVVMControl<VideoFlipViewItemModel>
{
    public VideoFlipViewItem()
    {
        this.InitializeComponentMVVM(updateBindingsAlways: true);
    }

    partial void ConnectToViewModel(VideoFlipViewItemModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        mediaPlayerElement.SetMediaPlayer(viewModel.MediaPlayer);
    }

    partial void DisconnectFromViewModel(VideoFlipViewItemModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        mediaPlayerElement.SetMediaPlayer(null);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.MediaPlayer))
        {
            mediaPlayerElement.SetMediaPlayer(ViewModel!.MediaPlayer);
        }
    }

    private void MediaPlayerElement_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ViewModel!.IsContextMenuEnabeld)
        {
            mediaPlayerElement.ShowAttachedFlyout(args);
        }
    }
}
