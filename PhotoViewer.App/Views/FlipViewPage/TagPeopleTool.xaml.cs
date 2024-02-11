using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoVieweApp.Utils;
using PhotoViewer.App.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.System;

namespace PhotoViewer.App.Views;
public sealed partial class TagPeopleTool : UserControl, IMVVMControl<TagPeopleToolModel>
{
    private const double DefaultFaceBoxSize = 100;

    private static readonly TimeSpan DefaultSelectionTimeSpan = TimeSpan.FromMilliseconds(500);

    private DateTime selectionStartTime = DateTime.MinValue;

    private string? contextRequestedName;

    public TagPeopleTool()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(TagPeopleToolModel viewModel)
    {
        viewModel.Subscribe(this, nameof(ViewModel.SelectionRectInPercent), ViewModel_SelectionRectChanged, initialCallback: true);
        viewModel.Subscribe(this, nameof(ViewModel.IsNameInputVisible), ViewModel_IsNameInputVisibleChanged, initialCallback: true);
        viewModel.Subscribe(this, nameof(ViewModel.UIScaleFactor), UpdateAutoSuggestBoxContainerPosition);
    }

    partial void DisconnectFromViewModel(TagPeopleToolModel viewModel)
    {
        viewModel.UnsubscribeAll(this);
    }

    private void ViewModel_SelectionRectChanged()
    {
        if (ViewModel!.SelectionRectInPercent != Rect.Empty)
        {
            UpdateSelectionRectBounds(ViewModel.SelectionRectInPercent);
            selectionRect.Visibility = Visibility.Visible;

            if (ViewModel.IsNameInputVisible)
            {
                UpdateAutoSuggestBoxContainerPosition();
            }
        }
        else
        {
            selectionRect.Visibility = Visibility.Collapsed;
        }
    }

    private void ViewModel_IsNameInputVisibleChanged()
    {
        if (ViewModel!.IsNameInputVisible)
        {
            autoSuggestBoxContainer.Visibility = Visibility.Visible;
            UpdateAutoSuggestBoxContainerPosition();
            FocusAutoSuggestBox();
        }
        else
        {
            autoSuggestBoxContainer.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateSelectionRectBounds(Rect selectionInPercent)
    {
        double x = selectionInPercent.X * ActualWidth;
        double y = selectionInPercent.Y * ActualHeight;
        double width = selectionInPercent.Width * ActualWidth;
        double height = selectionInPercent.Height * ActualHeight;
        selectionRect.SetBounds(new Rect(x, y, width, height));
    }

    private void PeopleTag_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel != null && !ViewModel.IsSelectionEnabled)
        {
            ((PeopleTagViewModel)((FrameworkElement)sender).DataContext).IsVisible = true;
        }
    }

    private void PeopleTag_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel != null && !ViewModel.IsSelectionEnabled)
        {
            ((PeopleTagViewModel)((FrameworkElement)sender).DataContext).IsVisible = false;
        }
    }

    private void SelectionCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        selectionCanvas.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.Cross));
    }

    private void SelectionCanvas_PointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (ViewModel!.IsSelectionEnabled)
        {
            var point = args.GetCurrentPoint((UIElement)sender);
            if (point.Properties.IsLeftButtonPressed)
            {
                selectionStartTime = DateTime.Now;
                ViewModel.OnUserStartedSelection();
                selectionRect.SetBounds(new Rect(point.Position.X, point.Position.Y, 0, 0));
                selectionRect.HandOverPointerPressedEvent(args);
            }
        }
    }

    private void SelectionCanvas_PointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (ViewModel!.IsSelectionEnabled)
        {
            var point = args.GetCurrentPoint((UIElement)sender);

            var bounds = selectionRect.GetBounds();

            if (bounds.Width <= 1 && bounds.Height <= 1
                && DateTime.Now - selectionStartTime < DefaultSelectionTimeSpan)
            {
                double x = Math.Max(0, point.Position.X - DefaultFaceBoxSize / 2);
                double y = Math.Max(0, point.Position.Y - DefaultFaceBoxSize / 2);
                double width = DefaultFaceBoxSize;
                double height = DefaultFaceBoxSize;
                selectionRect.SetBounds(new Rect(x, y, width, height));
                ViewModel!.SelectionRectInPercent = GetSelectionRectInPercent();
                ViewModel.OnUserEndedSelection();
            }
        }
    }

    private void SelectionRect_InteractionStarted(SelectionRect sender, EventArgs args)
    {
        ViewModel!.OnUserStartedSelection();
    }

    private void SelectionRect_InteractionEnded(SelectionRect sender, EventArgs args)
    {
        ViewModel!.SelectionRectInPercent = GetSelectionRectInPercent();
        ViewModel!.OnUserEndedSelection();
    }

    private void AutoSuggestBoxContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateAutoSuggestBoxContainerPosition();

        if (e.PreviousSize.Width == 0 && e.PreviousSize.Height == 0)
        {
            FocusAutoSuggestBox();
        }
    }

    private Rect GetSelectionRectInPercent()
    {
        var bounds = selectionRect.GetBounds();
        return new Rect(
            bounds.X / selectionCanvas.ActualWidth,
            bounds.Y / selectionCanvas.ActualHeight,
            bounds.Width / selectionCanvas.ActualWidth,
            bounds.Height / selectionCanvas.ActualHeight);
    }

    private void UpdateAutoSuggestBoxContainerPosition()
    {
        if (ViewModel is null)
        {
            return;
        }

        var selectionRectBounds = selectionRect.GetBounds();

        double autoSuggestBoxContainerWidth = autoSuggestBoxContainer.ActualWidth * ViewModel.UIScaleFactor;

        double left = selectionRectBounds.GetCenterX() - autoSuggestBoxContainerWidth / 2;
        left = Math.Clamp(left, 0, selectionCanvas.ActualWidth - autoSuggestBoxContainerWidth);
        Canvas.SetLeft(autoSuggestBoxContainer, left);

        if (selectionRectBounds.GetCenterY() < selectionCanvas.ActualHeight / 2)
        {
            Canvas.SetTop(autoSuggestBoxContainer, selectionRectBounds.Top + selectionRectBounds.Height);
            AutoSuggestBoxExtension.SetSuggestionListDirection(autoSuggestBox, SuggestionListDirection.Down);
            autoSuggestBoxContainer.RenderTransformOrigin = new Point(0, 0);
        }
        else
        {
            Canvas.SetTop(autoSuggestBoxContainer, selectionRectBounds.Top - autoSuggestBoxContainer.ActualHeight);
            AutoSuggestBoxExtension.SetSuggestionListDirection(autoSuggestBox, SuggestionListDirection.Up);
            autoSuggestBoxContainer.RenderTransformOrigin = new Point(0, 1);
        }
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox autoSuggestBox, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.SuggestionChosen)
        {
            autoSuggestBox.ItemsSource = ViewModel!.FindSuggestions(autoSuggestBox.Text);
        }
    }

    private void AutoSuggestBox_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        var autoSuggestBox = (AutoSuggestBox)sender;

        if (!autoSuggestBox.IsSuggestionListOpen)
        {
            if (autoSuggestBox.Text == string.Empty)
            {
                autoSuggestBox.ItemsSource = ViewModel!.GetRecentSuggestions();
            }
            else
            {
                autoSuggestBox.ItemsSource = ViewModel!.FindSuggestions(autoSuggestBox.Text);
            }

            DispatcherQueue.TryEnqueue(() => autoSuggestBox.IsSuggestionListOpen = true);
        }
    }

    private void AutoSuggestBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Tab && !e.IsModifierPressed(VirtualKey.Shift))
        {
            ViewModel!.TrySelectNextDetectedFace();
            e.Handled = true;
        }
        if (e.Key == VirtualKey.Escape && !autoSuggestBox.IsSuggestionListOpen)
        {
            ViewModel!.ExitPeopleTagging();
            e.Handled = true;
        }
    }

    private void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(autoSuggestBox.Text)
            && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right))
        {
            e.Handled = true; // prevent bubble to flip view
        }
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        sender.RunWhenTextChanged(args.QueryText, () => ViewModel!.AddPersonCommand.TryExecute());         
    }

    private void FocusAutoSuggestBox()
    {
        if (autoSuggestBox.FocusState == FocusState.Unfocused)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                autoSuggestBox.Focus(FocusState.Programmatic);
            });
        }
    }

    private void SelectionCanvas_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (args.TryGetPosition(selectionCanvas, out var position))
        {
            var percentagePosition = new Point(
                position.X / selectionCanvas.ActualWidth,
                position.Y / selectionCanvas.ActualHeight);

            if (ViewModel!.TaggedPeople.FirstOrDefault(vm => vm.FaceBox.Contains(percentagePosition)) is { } peopleTagVM)
            {
                args.Handled = true;
                selectionCanvas.ShowAttachedFlyout(args);
                contextRequestedName = peopleTagVM.Name;
            }
        }
    }

    private void SelectionCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ViewModel != null && !ViewModel.SelectionRectInPercent.IsEmpty)
        {
            UpdateSelectionRectBounds(ViewModel.SelectionRectInPercent);
            UpdateAutoSuggestBoxContainerPosition();
        }
    }

    private void RemovePeopleTagMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ViewModel!.RemovePeopleTagCommand.Execute(contextRequestedName);
    }

    private async void AutoSuggestBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        if (ViewModel!.SelectionRectInPercent.IsEmpty)
        {
            // set focus to flipview when hiding autosuggestbox
            var flipView = this.FindParent<FlipView>()!;
            if (!args.TrySetNewFocusedElement(flipView))
            {
                await flipView.TryFocusAsync();
            }
        }
    }

}
