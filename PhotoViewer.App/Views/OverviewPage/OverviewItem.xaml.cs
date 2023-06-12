using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Utils;
using System;
using Windows.Foundation;
using System.Linq;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoViewer.App.Converters;
using PhotoViewer.Core.Models;
using Microsoft.UI.Xaml.Input;
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
    }

    partial void DisconnectFromViewModel(OverviewItemModel viewModel)
    {
        viewModel.ThumbnailInvalidated -= ViewModel_ThumbnailInvalidated;
    }

    private async void ViewModel_ThumbnailInvalidated(object? sender, EventArgs e)
    {
        Log.Info($"Reload thumbnail for {ViewModel!.MediaFile.DisplayName}");
        image.Source = await MediaFileInfoToThumbnailConverter.ConvertAsync(ViewModel.MediaFile);
    }

    private async void ToolTip_Opened(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            if (ViewModel.MediaFile is IBitmapFileInfo) // TODO vector graphics and videos
            {
                try
                {
                    var bitmapImage = new BitmapImage();
                    toolTipImage.Source = bitmapImage;
                    using var stream = await ViewModel.MediaFile.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);
                    await bitmapImage.SetSourceAsync(stream);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to load image.", ex);
                }
            }
        }
        else
        {
            Log.Error("Could not show image preview because view model was null.");
        }
    }

    private void ToolTip_Closed(object sender, RoutedEventArgs e)
    {
        toolTipImage.Source = null;
    }

    private string ConvertRatingToStars(int rating)
    {
        return string.Join(" ", Enumerable.Repeat('\uE00A', rating));
    }

    private void FileNameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            ViewModel?.CancelRenaming();
        }
        else if (e.Key == VirtualKey.Enter)
        {
            ViewModel?.ConfirmRenaming();
        }
        else if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right
            || e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
        {
            e.Handled = true;
        }
    }

    private void FileNameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel?.ConfirmRenaming();
    }

}
