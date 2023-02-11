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
using PhotoViewer.App.Models;
using PhotoViewer.App.Converters;

namespace PhotoViewer.App.Views;

public sealed partial class OverviewItem : UserControl, IMVVMControl<OverviewItemModel>
{
    public OverviewItemModel? ViewModel => DataContext as OverviewItemModel;

    public OverviewItem()
    {
        this.InitializeComponentMVVM();
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
        Log.Info($"Reload thumbnail for {ViewModel!.MediaFile.Name}");
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
                    using var stream = await ViewModel.MediaFile.OpenAsync(FileAccessMode.Read);
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
}
