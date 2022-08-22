using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace PhotoViewerApp.Views;

public sealed partial class FlipViewPage : Page
{
    private FlipViewPageModel ViewModel { get; } = FlipViewPageModelFactory.Create();

    public FlipViewPage()
    {
        this.InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedItem))
        {
            WindowsManger.GetForCurrentThread().Title = ViewModel.SelectedItem?.Name ?? "";
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
