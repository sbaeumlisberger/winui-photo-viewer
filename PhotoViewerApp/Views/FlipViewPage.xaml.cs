using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System.ComponentModel;

namespace PhotoViewerApp.Views;

public sealed partial class FlipViewPage : Page
{
    private FlipViewPageModel ViewModel { get; } = FlipViewPageModelFactory.Create(App.Current.Window.DialogService);

    public FlipViewPage()
    {
        this.InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedItem))
        {
            App.Current.Window.Title = ViewModel.SelectedItem?.Name ?? "";
        }
    }
    private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (flipView.SelectedItem is IMediaItem mediaItem && flipView.ContainerFromItem(mediaItem) is FlipViewItem fipViewItem)
        {
            var viewModel = (MediaFlipViewItemModel?)ViewModel.GetFlipViewItemModel(mediaItem);
            ((MediaFlipViewItem)fipViewItem.ContentTemplateRoot).ViewModel = viewModel;
        }
    }

    private void MediaFlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is IMediaItem mediaItem)
        {
            ((MediaFlipViewItem)sender).ViewModel = (MediaFlipViewItemModel?)ViewModel.GetFlipViewItemModel(mediaItem);
        }
    }


}
