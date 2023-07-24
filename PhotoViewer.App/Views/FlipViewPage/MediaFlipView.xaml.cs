using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoVieweApp.Utils;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Utils;
using Windows.System;

namespace PhotoViewer.App.Views;

public sealed partial class MediaFlipView : UserControl, IMVVMControl<MediaFlipViewModel>
{
    public MediaFlipView()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(MediaFlipViewModel viewModel)
    {
        viewModel.DeleteAnimationRequested += this.ViewModel_DeleteAnimationRequested;
    }

    partial void DisconnectFromViewModel(MediaFlipViewModel viewModel)
    {
        viewModel.DeleteAnimationRequested -= this.ViewModel_DeleteAnimationRequested;
    }

    private void ViewModel_DeleteAnimationRequested(object? sender, AsyncEventArgs e)
    {
        e.AddTask(DeleteStoryboard.RunAsync());
    }

    private void FlipView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ViewModel!.IsDiashowActive)
        {
            flipView.ShowAttachedFlyout(args);
        }
    }

    private void FlipView_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (ViewModel!.IsDiashowActive)
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
        Log.Debug("FlipView_SelectionChanged " + flipView.SelectedIndex);

        if (e.AddedItems.Count == 1 && e.RemovedItems.Count == 1)
        {
            // update view model when selection was changed by the user
            ViewModel!.Select((IMediaFileInfo?)flipView.SelectedItem);
        }
        else if (ViewModel!.SelectedItem != null && flipView.SelectedItem != ViewModel.SelectedItem)
        {
            DispatcherQueue.TryEnqueue(() => flipView.SelectedItem = ViewModel.SelectedItem);
        }

        FocusSelectedItem();

        flipView.Opacity = 1; // reset delete animation 
    }

    private void FlipViewItem_Loaded(object sender, RoutedEventArgs e)
    {
        FocusSelectedItem();
    }

    private void FlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender.DataContext is IMediaFileInfo mediaFile)
        {
            sender.DataContext = ViewModel?.TryGetItemModel(mediaFile);
        }
    }

    private void FocusSelectedItem()
    {
        if (flipView.SelectedItem is { } selectedItem
            && flipView.ContainerFromItem(selectedItem) is FlipViewItem flipViewItem)
        {
            flipViewItem.Focus(FocusState.Programmatic);
        }
    }

    private void FlipView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        /* keyboard accelerators in an attached flyout are not working, 
         * therefore they are executed via key down event handler */

        if (e.Key == VirtualKey.Space)
        {
            ViewModel!.ToogleDiashowLoopCommand.TryExecute();
        }
        else if (e.Key == VirtualKey.Escape)
        {
            ViewModel!.ExitDiashowCommand.TryExecute();
        }
    }

    private void FlipView_PrepareContainer(object sender, (DependencyObject Element, object Item) e)
    {
        if (e.Item is IMediaFileInfo mediaFile)
        {
            var flipViewItem = (FlipViewItem)e.Element;
            if (flipViewItem.ContentTemplateRoot is FrameworkElement root)
            {
                var itemModel = ViewModel?.TryGetItemModel(mediaFile);
                root.DataContext = itemModel;
            }
        }
    }
}
