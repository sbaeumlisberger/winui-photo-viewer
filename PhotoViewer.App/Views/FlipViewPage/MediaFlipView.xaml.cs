using Essentials.NET;
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
        InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(MediaFlipViewModel viewModel)
    {
        viewModel.DeleteAnimationRequested += ViewModel_DeleteAnimationRequested;
    }

    partial void DisconnectFromViewModel(MediaFlipViewModel viewModel)
    {
        viewModel.DeleteAnimationRequested -= ViewModel_DeleteAnimationRequested;
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
            var windowCenterX = window.Bounds.GetCenterX();

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
        Log.Debug("FlipView_SelectionChanged: SelectedIndex=" + flipView.SelectedIndex + ", SelectedItem=" + flipView.SelectedItem);
       
        if (e.AddedItems.Count == 1 && e.RemovedItems.Count == 1)
        {
            // update view model when selection was changed by the user
            ViewModel!.Select((IMediaFileInfo?)flipView.SelectedItem);
        }
        else if (ViewModel!.SelectedItem != null && flipView.SelectedItem != ViewModel.SelectedItem)
        {
            Log.Error("Invalid FlipView Selection"); // that should not more happen
            Log.Error("ViewModel.SelectedIndex=" + ViewModel!.Items.IndexOf(ViewModel.SelectedItem!));
            Log.Error("ViewModel.SelectedItem=" + ViewModel!.SelectedItem);
            DispatcherQueue.TryEnqueue(() => flipView.SelectedItem = ViewModel.SelectedItem);
        }

        flipView.Opacity = 1; // reset delete animation
    }

    private void FlipViewItemBorder_Loaded(object sender, RoutedEventArgs e)
    {
        FocusSelectedItem();
    }

    private void FocusSelectedItem()
    {
        if (flipView.ContainerFromItem(flipView.SelectedItem) is FrameworkElement container)
        {
            container.Focus(FocusState.Programmatic);
        }
    }

    private void FlipViewItemBorder_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        var border = (Border)sender;

        if (border.DataContext is IMediaFileInfo mediaFile)
        {
            var flipViewItem = (FrameworkElement)border.Child;
            flipViewItem.DataContext = ViewModel?.TryGetItemModel(mediaFile);
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

    private void FlipView_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        Log.Debug("FlipView_LosingFocus " + args.FocusState + ", " + args.InputDevice + ", " + args.NewFocusedElement);
    }
}
