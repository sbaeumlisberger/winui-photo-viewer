using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoVieweApp.Utils;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using System.Linq;

namespace PhotoViewer.App.Views;

public sealed partial class MediaFlipView : UserControl, IMVVMControl<MediaFlipViewModel>
{
    private MediaFlipViewModel ViewModel => (MediaFlipViewModel)DataContext;

    public MediaFlipView()
    {
        this.InitializeComponentMVVM();
    }

    private void FlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender.DataContext is IMediaFileInfo mediaFile
            && ViewModel?.TryGetItemModel(mediaFile) is { } itemModel)
        {
            Log.Debug($"apply item model {itemModel.MediaItem.DisplayName} to flipview item");
            sender.DataContext = itemModel;
        }
    }

    private void FlipView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ViewModel.IsDiashowActive)
        {
            flipView.ShowAttachedFlyout(args);
        }
    }

    private void FlipView_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (ViewModel.IsDiashowActive)
        {
            var window = App.Current.Window;
            var windowCenterX = window.Bounds.Left + window.Bounds.Width / 2;

            if (e.GetPosition(window.Content).X > windowCenterX)
            {
                ViewModel.SelectNext();
            }
            else
            {
                ViewModel.SelectPrevious();
            }
        }
    }

    private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyItemsModels();
        FocusSelectedItem();

        if (e.AddedItems.Count == 1 && e.RemovedItems.Count == 1)
        {
            // update view model when selection was changed by the user
            ViewModel.Select((IMediaFileInfo?)flipView.SelectedItem);
        }     
    }

    private void FlipViewItem_Loaded(object sender, RoutedEventArgs e)
    {
        FocusSelectedItem();
    }

    private void FocusSelectedItem()
    {
        if (flipView.SelectedItem is { } selectedItem
            && flipView.ContainerFromItem(selectedItem) is FlipViewItem flipViewItem)
        {
            flipViewItem.Focus(FocusState.Programmatic);
        }
    }

    private void ApplyItemsModels() 
    {
        Log.Debug($"apply item models");
        foreach (var itemModel in ViewModel.ItemModels)
        {
            if (flipView.ContainerFromItem(itemModel.MediaItem) is FlipViewItem flipViewItem)
            {
                Log.Debug($"apply item model {itemModel.MediaItem.DisplayName} to flipview item");
                ((FrameworkElement)flipViewItem.ContentTemplateRoot).DataContext = itemModel;
            }
        }
    }

}
