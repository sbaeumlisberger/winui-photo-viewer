using MetadataAPI.Data;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PhotoVieweApp.Utils;
using PhotoViewer.Core.ViewModels;
using PhotoViewerApp.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace PhotoViewerApp.Views;
public sealed partial class TagPeopleTool : UserControl, IMVVMControl<TagPeopleToolModel>
{
    private const double DefaultFaceBoxSize = 100;

    private TagPeopleToolModel ViewModel => (TagPeopleToolModel)DataContext;

    public TagPeopleTool()
    {
        this.InitializeMVVM(ConnectToViewModel, DisconnectFromViewModel);
    }

    private void ConnectToViewModel(TagPeopleToolModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void DisconnectFromViewModel(TagPeopleToolModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectionRect))
        {
            if (ViewModel.SelectionRect != default)
            {
                Canvas.SetLeft(selectionRect, ViewModel.SelectionRect.X * ActualWidth);
                Canvas.SetTop(selectionRect, ViewModel.SelectionRect.Y * ActualHeight);
                selectionRect.Width = ViewModel.SelectionRect.Width * ActualWidth;
                selectionRect.Height = ViewModel.SelectionRect.Height * ActualHeight;
                selectionRect.Visibility = Visibility.Visible;
                UpdateAutoSuggestBoxContainerPosition();
                autoSuggestBoxContainer.Visibility = Visibility.Visible;
                autoSuggestBox.Focus(FocusState.Programmatic);
            }
            else
            {
                selectionRect.Visibility = Visibility.Collapsed;
                autoSuggestBoxContainer.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void PeopleTagUI_Loaded(object sender, RoutedEventArgs e)
    {
        SetUpPeopleTagUI((ContentPresenter)VisualTreeHelper.GetParent((StackPanel)sender));
    }

    private void PeopleTagsCanvas_SizeChanged(object sender, SizeChangedEventArgs args)
    {
        foreach (ContentPresenter container in ((Canvas)sender).Children)
        {
            SetUpPeopleTagUI(container);
        }
    }

    private void SetUpPeopleTagUI(ContentPresenter container)
    {
        var peopleTag = (PeopleTagViewModel)container.DataContext;

        var stackPanel = (StackPanel)VisualTreeHelper.GetChild(container, 0);
        var faceBox = (FrameworkElement)stackPanel.Children[0];

        faceBox.Width = ActualWidth * peopleTag.Rectangle.Width;
        faceBox.Height = ActualHeight * peopleTag.Rectangle.Height;

        double leftOffset = Math.Max(0, container.ActualWidth - faceBox.Width) / 2;
        Canvas.SetLeft(container, ActualWidth * peopleTag.Rectangle.X - leftOffset);
        Canvas.SetTop(container, ActualHeight * peopleTag.Rectangle.Y);
    }

    private void PeopleTag_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!ViewModel.IsActive)
        {
            ((FrameworkElement)sender).Opacity = 1;
        }
    }

    private void PeopleTag_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!ViewModel.IsActive)
        {
            ((FrameworkElement)sender).Opacity = 0;
        }
    }

    private void SuggestedFaceUI_Loaded(object sender, RoutedEventArgs e)
    {
        SetUpSuggestedFaceUI((ContentPresenter)VisualTreeHelper.GetParent((FrameworkElement)sender));
    }

    private void SuggestedFacesCanvas_SizeChanged(object sender, SizeChangedEventArgs args)
    {
        foreach (ContentPresenter container in ((Canvas)sender).Children)
        {
            SetUpSuggestedFaceUI(container);
        }
    }

    private void SetUpSuggestedFaceUI(ContentPresenter container)
    {
        var faceRect = (Rect)container.DataContext;
        var faceBox = (Rectangle)VisualTreeHelper.GetChild(container, 0);
        faceBox.Width = ActualWidth * faceRect.Width;
        faceBox.Height = ActualHeight * faceRect.Height;
        Canvas.SetLeft(container, ActualWidth * faceRect.X);
        Canvas.SetTop(container, ActualHeight * faceRect.Y);
    }

    private void SelectionCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        selectionCanvas.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.Cross));
    }

    private void SelectionCanvas_PointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (ViewModel.IsActive)
        {
            ViewModel.SelectionRect = default;

            var point = args.GetCurrentPoint((UIElement)sender);
            if (point.Properties.IsLeftButtonPressed)
            {
                Canvas.SetLeft(selectionRect, point.Position.X);
                Canvas.SetTop(selectionRect, point.Position.Y);
                selectionRect.Width = 0;
                selectionRect.Height = 0;
                selectionRect.Visibility = Visibility.Visible;
                selectionRect.HandOverPointerPressedEvent(args);
            }
        }
    }

    private void SelectionCanvas_PointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (ViewModel.IsActive)
        {
            var point = args.GetCurrentPoint((UIElement)sender);

            if (selectionRect.Width == 0 && selectionRect.Height == 0)
            {
                Canvas.SetLeft(selectionRect, Math.Max(0, point.Position.X - DefaultFaceBoxSize / 2));
                Canvas.SetTop(selectionRect, Math.Max(0, point.Position.Y - DefaultFaceBoxSize / 2));
                selectionRect.Width = DefaultFaceBoxSize;
                selectionRect.Height = DefaultFaceBoxSize;
                ViewModel.SelectionRect = GetSelectionRectInPercent();
            }
        }
    }

    private void SelectionRect_InteractionStarted(SelectionRect sender, EventArgs args)
    {
        ViewModel.ClearSuggestedFaces();
        autoSuggestBoxContainer.Visibility = Visibility.Collapsed;
    }

    private void SelectionRect_InteractionEnded(SelectionRect sender, EventArgs args)
    {
        autoSuggestBoxContainer.Visibility = Visibility.Visible;
        ViewModel.SelectionRect = GetSelectionRectInPercent();
    }

    private Rect GetSelectionRectInPercent()
    {
        var bounds = selectionRect.GetBounds();
        return new Rect(bounds.X / ActualWidth, bounds.Y / ActualHeight,
                        bounds.Width / ActualWidth, bounds.Height / ActualHeight);
    }

    private void UpdateAutoSuggestBoxContainerPosition()
    {
        var selectionRectBounds = selectionRect.GetBounds();

        double left = selectionRectBounds.Left + selectionRect.Width / 2 - autoSuggestBoxContainer.ActualWidth / 2;
        left = Math.Min(Math.Max(left, 0), ActualWidth - autoSuggestBoxContainer.ActualWidth);
        Canvas.SetLeft(autoSuggestBoxContainer, left);

        double selectionRectCenterY = selectionRectBounds.Top + selectionRectBounds.Height / 2;

        if (selectionRectCenterY < ActualHeight / 2)
        {
            Canvas.SetTop(autoSuggestBoxContainer, selectionRectBounds.Top + selectionRect.Height);
            AutoSuggestBoxExtension.SetSuggestionListDirection(autoSuggestBox, SuggestionListDirection.Down);
        }
        else
        {
            Canvas.SetTop(autoSuggestBoxContainer, selectionRectBounds.Top - autoSuggestBoxContainer.ActualHeight);
            AutoSuggestBoxExtension.SetSuggestionListDirection(autoSuggestBox, SuggestionListDirection.Up);
        }
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = ViewModel.FindSuggestions(sender.Text);
        }
    }

    private void AutoSuggestBox_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        var autoSuggestBox = (AutoSuggestBox)sender;

        if (autoSuggestBox.Text == string.Empty)
        {
            autoSuggestBox.ItemsSource = ViewModel.GetRecentSuggestions();

            DispatcherQueue.TryEnqueue(() =>
            {
                autoSuggestBox.IsSuggestionListOpen = true;
            });
        }
    }

    private void AutoSuggestBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Tab)
        {
            e.Handled = ViewModel.SkipCurrentDetectedFace();
        }
        if (e.Key == VirtualKey.Escape && !autoSuggestBox.IsSuggestionListOpen)
        {
            ViewModel.ExitPeopleTagging();
            e.Handled = true;
        }
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if(args.ChosenSuggestion is null) // user pressed enter or clicked query button
        {
            ViewModel.AddPersonCommand.Execute(null);
        }
    }
}
