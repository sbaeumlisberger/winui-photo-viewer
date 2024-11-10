using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.System;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace PhotoViewer.App.Views;

public sealed partial class MediaFlipView : UserControl, IMVVMControl<MediaFlipViewModel>
{
    public MediaFlipView()
    {
        InitializeComponentMVVM();
        Loaded += MediaFlipView_Loaded;
    }

    private void MediaFlipView_Loaded(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            FindName(nameof(selectedItemIndicator));
        });
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

            if (ViewModel.SelectedItem is not null && flipView.ContainerFromItem(ViewModel.SelectedItem) is ContentControl container)
            {
                var flipViewItem = (FrameworkElement)((Border)container.ContentTemplateRoot).Child;
                flipViewItem.DataContext = ViewModel.TryGetItemModel(ViewModel.SelectedItem);
            }
        }
        else if (ViewModel!.SelectedItem != null && flipView.SelectedItem != ViewModel.SelectedItem)
        {
            Log.Debug("invalid selection change detected: flipView.SelectedItem=" + flipView.SelectedItem + ", ViewModel.SelectedItem=" + ViewModel!.SelectedItem);
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

    private SolidColorBrush ToFlipViewBackground(bool isDiashowActive)
    {
        return new SolidColorBrush(isDiashowActive ? Colors.Black : Colors.Transparent);
    }

    private SymbolIcon ToDiashowLoopMenuItemIcon(bool isDiashowLoopActive)
    {
        return new SymbolIcon(isDiashowLoopActive ? Symbol.Pause : Symbol.Play);
    }

    private string ToDiashowLoopMenuItemText(bool isDiashowLoopActive)
    {
        return isDiashowLoopActive ? Strings.MediaFlipView_DisableDiashowLoop : Strings.MediaFlipView_EnableDiashowLoop;
    }

    private async void InfoBar_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        if (!args.TrySetNewFocusedElement(flipView))
        {
            await flipView.TryFocusAsync();
        }
    }
}
