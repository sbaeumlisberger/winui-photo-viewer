using Essentials.NET.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewer.App.Converters;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.IO;
using System.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.App.Views;

public sealed partial class OverviewItem : UserControl, IMVVMControl<OverviewItemModel>
{
    public OverviewItem()
    {
        this.InitializeComponentMVVM();

        fileNameTextBox.RegisterPropertyChangedCallback(VisibilityProperty, (obj, dp) =>
        {
            if (fileNameTextBox.Visibility == Visibility.Visible)
            {
                fileNameTextBox.Focus(FocusState.Programmatic);
            }
        });
    }

    partial void ConnectToViewModel(OverviewItemModel viewModel)
    {
        viewModel.ThumbnailInvalidated += ViewModel_ThumbnailInvalidated;
        image.Source = MediaFileInfoToThumbnailConverter.Convert(viewModel.MediaFile);
    }

    partial void DisconnectFromViewModel(OverviewItemModel viewModel)
    {
        viewModel.ThumbnailInvalidated -= ViewModel_ThumbnailInvalidated;
        image.Source = null;
    }

    private void ViewModel_ThumbnailInvalidated(object? sender, EventArgs e)
    {
        Log.Info($"Reload thumbnail for {ViewModel!.MediaFile.DisplayName}");
        image.Source = MediaFileInfoToThumbnailConverter.Convert(ViewModel.MediaFile);
    }

    private async void ToolTip_Opened(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            Log.Error("Could not show preview because view model was null.");
            return;
        }

        try
        {
            Log.Debug($"Show preview for {ViewModel.MediaFile.DisplayName}");

            if (ViewModel.MediaFile is IBitmapFileInfo bitmapFile)
            {
                var bitmapImage = new BitmapImage();
                var image = new Image() { Source = bitmapImage, MaxWidth = 600, MaxHeight = 600 };
                toolTipPreview.Child = image;
                using var stream = await bitmapFile.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);
                await bitmapImage.SetSourceAsync(stream);
            }
            else if (ViewModel.MediaFile is IVectorGraphicFileInfo vectorGraphicFile)
            {
                var webView = new WebView2() { Width = 600, Height = 400 };
                toolTipPreview.Child = webView;
                await webView.EnsureCoreWebView2Async();
                using var stream = await vectorGraphicFile.OpenAsync(FileAccessMode.Read);
                string svgString = await new StreamReader(stream).ReadToEndAsync();
                webView.NavigateToString(SvgUtil.EmbedInHtml(svgString));
            }
            else if (ViewModel.MediaFile is IVideoFileInfo videoFile)
            {
                var mediaPlayerElement = new MediaPlayerElement() { Width = 600, Height = 400 };
                toolTipPreview.Child = mediaPlayerElement;
                var stream = await videoFile.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);
                var mediaSource = MediaSource.CreateFromStream(stream, videoFile.ContentType);
                var mediaPlayer = new MediaPlayer() { Source = mediaSource, AutoPlay = true };
                mediaPlayerElement.SetMediaPlayer(mediaPlayer);
                mediaPlayerElement.Unloaded += (_, _) =>
                {
                    mediaPlayerElement.SetMediaPlayer(null);
                    mediaPlayer.Dispose();
                    mediaSource.Dispose();
                    stream.Dispose();
                };
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load image.", ex);
        }
    }

    private void ToolTip_Closed(object sender, RoutedEventArgs e)
    {
        toolTipPreview.Child = null;
    }

    private string ConvertRatingToStars(int rating)
    {
        return string.Join(" ", Enumerable.Repeat('\uE00A', rating));
    }

    private async void FileNameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            ViewModel!.CancelRenaming();
        }
        else if (e.Key == VirtualKey.Enter)
        {
            await ViewModel!.ConfirmRenaming();
        }
        else if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right
            || e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
        {
            e.Handled = true;
        }
    }

    private async void FileNameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel!.IsRenaming)
        {
            await ViewModel.ConfirmRenaming();
        }
    }
}
