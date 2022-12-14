using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using PhotoVieweApp.Utils;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace PhotoViewerApp.Views;

public sealed partial class VideoFlipViewItem : UserControl, IMVVMControl<VideoFlipViewItemModel>
{
    private VideoFlipViewItemModel ViewModel => (VideoFlipViewItemModel)DataContext;

    public VideoFlipViewItem()
    {
        this.InitializeMVVM(
           connectToViewModel: viewModel =>
           {
               mediaPlayerElement.SetMediaPlayer(viewModel.MediaPlayer);
           },
           disconnectFromViewModel: viewModel =>
           {
               mediaPlayerElement.SetMediaPlayer(null);
           });
    }

    private void MediaPlayerElement_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ViewModel.IsContextMenuEnabeld)
        {
            mediaPlayerElement.ShowAttachedFlyout(args);
        }
    }
}
