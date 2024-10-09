using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.ComponentModel;

namespace PhotoViewer.App.Views;

public sealed partial class VideoFlipViewItem : UserControl, IMVVMControl<VideoFlipViewItemModel>
{
    public VideoFlipViewItem()
    {
        this.InitializeComponentMVVM();
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

    private void MediaTransportControls_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }
}
