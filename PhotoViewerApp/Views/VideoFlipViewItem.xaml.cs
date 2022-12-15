using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace PhotoViewerApp.Views;

public sealed partial class VideoFlipViewItem : UserControl
{
    private VideoFlipViewItemModel ViewModel => (VideoFlipViewItemModel)DataContext;

    public VideoFlipViewItem()
    {
        this.InitializeMVVM<VideoFlipViewItemModel>(InitializeComponent,
           connectToViewModel: viewModel =>
           {
               this.Bindings.Update();
               mediaPlayerElement.SetMediaPlayer(viewModel.MediaPlayer);
           },
           disconnectFromViewModel: viewModel =>
           {
               this.Bindings.StopTracking();
               mediaPlayerElement.SetMediaPlayer(null);
           });
    }

}
