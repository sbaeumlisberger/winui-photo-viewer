using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoVieweApp.Utils;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Controls;
using PhotoViewer.App.Utils;
using System;
using System.ComponentModel;
using Windows.Foundation;
using Windows.System;
using System.Linq;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PhotoViewer.App.Views;
public sealed partial class TagPeopleTool : UserControl, IMVVMControl<TagPeopleToolModel>
{
    private const double DefaultFaceBoxSize = 100;

    private string? contextRequestedName;

    public TagPeopleTool()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(TagPeopleToolModel viewModel)
    {
        viewModel.Subscribe(this, nameof(ViewModel.SelectionRect), ViewModel_SelectionRectChanged);
    }

    partial void DisconnectFromViewModel(TagPeopleToolModel viewModel)
    {
        viewModel.UnsubscribeAll(this);
    }

    private void ViewModel_SelectionRectChanged()
    {
        if (ViewModel!.SelectionRect != Rect.Empty)
        {
            Canvas.SetLeft(selectionRect, ViewModel.SelectionRect.X * ActualWidth);
            Canvas.SetTop(selectionRect, ViewModel.SelectionRect.Y * ActualHeight);
            selectionRect.Width = ViewModel.SelectionRect.Width * ActualWidth;
            selectionRect.Height = ViewModel.SelectionRect.Height * ActualHeight;
            selectionRect.Visibility = Visibility.Visible;

            autoSuggestBoxContainer.Visibility = Visibility.Visible;
            UpdateAutoSuggestBoxContainerPosition();
            FocusAutoSuggestBox();
        }
        else
        {
            selectionRect.Visibility = Visibility.Collapsed;
            autoSuggestBoxContainer.Visibility = Visibility.Collapsed;
        }
    }

    private void PeopleTag_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!ViewModel!.IsSelectionEnabled)
        {
            ((PeopleTagViewModel)((FrameworkElement)sender).DataContext).IsVisible = true;
        }
    }

    private void PeopleTag_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!ViewModel!.IsSelectionEnabled)
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
                ViewModel.OnUserStartedSelection();
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
        if (ViewModel!.IsSelectionEnabled)
        {
            var point = args.GetCurrentPoint((UIElement)sender);

            if (selectionRect.Width == 0 && selectionRect.Height == 0)
            {
                Canvas.SetLeft(selectionRect, Math.Max(0, point.Position.X - DefaultFaceBoxSize / 2));
                Canvas.SetTop(selectionRect, Math.Max(0, point.Position.Y - DefaultFaceBoxSize / 2));
                selectionRect.Width = DefaultFaceBoxSize;
                selectionRect.Height = DefaultFaceBoxSize;
                ViewModel.OnUserEndedSelection(GetSelectionRectInPercent());
            }
        }
    }

    private void SelectionRect_InteractionStarted(SelectionRect sender, EventArgs args)
    {
        ViewModel!.OnUserStartedSelection();
    }

    private void SelectionRect_InteractionEnded(SelectionRect sender, EventArgs args)
    {
        ViewModel!.OnUserEndedSelection(GetSelectionRectInPercent());
    }

    private void AutoSuggestBoxContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateAutoSuggestBoxContainerPosition();
        FocusAutoSuggestBox();
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
            autoSuggestBoxContainer.RenderTransformOrigin = new Point(0.5, 0);
        }
        else
        {
            Canvas.SetTop(autoSuggestBoxContainer, selectionRectBounds.Top - autoSuggestBoxContainer.ActualHeight);
            AutoSuggestBoxExtension.SetSuggestionListDirection(autoSuggestBox, SuggestionListDirection.Up);
            autoSuggestBoxContainer.RenderTransformOrigin = new Point(0.5, 1);
        }
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox autoSuggestBox, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen)
        {
            FocusAutoSuggestBox(); // set focus back to text box when suggestion chosen via mouse            
        }
        else
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
        if (args.ChosenSuggestion is null) // user pressed enter or clicked query button
        {
            ViewModel!.AddPersonCommand.Execute(null);
        }
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

    private void RemovePeopleTagMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ViewModel!.RemovePeopleTagCommand.Execute(contextRequestedName);
    }

    private void AutoSuggestBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        if (ViewModel!.SelectionRect.IsEmpty)
        {
            // set focus to flipview when hiding autosuggestbox
            args.TrySetNewFocusedElement(this.FindParent<FlipView>()!);
        }
    }
}
