using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;

namespace PhotoViewerApp.Utils;
public enum SuggestionListDirection
{
    Auto,
    Down,
    Up
}

public static class AutoSuggestBoxExtension
{
    public static readonly DependencyProperty AllowPopupToOverflowProperty = DependencyPropertyHelper.RegisterAttached(
       typeof(AutoSuggestBoxExtension), nameof(AllowPopupToOverflowProperty), false, OnAllowPopupToOverflowChanged);

    public static readonly DependencyProperty SuggestionListDirectionProperty = DependencyPropertyHelper.RegisterAttached(
       typeof(AutoSuggestBoxExtension), nameof(SuggestionListDirectionProperty), SuggestionListDirection.Auto, OnSuggestionListDirectionChanged);

    private static readonly DependencyProperty VerticalOffsetCallbackTokenProperty = DependencyPropertyHelper.RegisterAttached<long?>
        (typeof(AutoSuggestBoxExtension), nameof(VerticalOffsetCallbackTokenProperty), null);

    public static bool GetAllowPopupToOverflow(DependencyObject obj) => (bool)obj.GetValue(AllowPopupToOverflowProperty);
    public static void SetAllowPopupToOverflow(DependencyObject obj, bool value) => obj.SetValue(AllowPopupToOverflowProperty, value);

    public static SuggestionListDirection GetSuggestionListDirection(DependencyObject obj) => (SuggestionListDirection)obj.GetValue(SuggestionListDirectionProperty);
    public static void SetSuggestionListDirection(DependencyObject obj, SuggestionListDirection value) => obj.SetValue(SuggestionListDirectionProperty, value);

    private static void OnAllowPopupToOverflowChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        WhenLoaded((AutoSuggestBox)obj, autoSuggestBox =>
        {
            var suggestionsContainer = FindSuggestionsContainer(autoSuggestBox);

            if (GetAllowPopupToOverflow(autoSuggestBox))
            {
                suggestionsContainer.SizeChanged += SuggestionsContainer_SizeChanged;
            }
            else
            {
                suggestionsContainer.SizeChanged -= SuggestionsContainer_SizeChanged;
            }
        });
    }

    private static void OnSuggestionListDirectionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        WhenLoaded((AutoSuggestBox)obj, autoSuggestBox =>
        {
            var suggestionListDirection = GetSuggestionListDirection(autoSuggestBox);

            var popup = FindSuggestionsPopup(autoSuggestBox);

            if (autoSuggestBox.GetValue(VerticalOffsetCallbackTokenProperty) is long verticalOffsetCallbackToken)
            {
                popup.UnregisterPropertyChangedCallback(Popup.VerticalOffsetProperty, verticalOffsetCallbackToken);
            }

            if (suggestionListDirection == SuggestionListDirection.Down)
            {
                autoSuggestBox.SetValue(VerticalOffsetCallbackTokenProperty, popup.RegisterPropertyChangedCallback(Popup.VerticalOffsetProperty, (obj, dp) =>
                {
                    ListView listView = (ListView)((Border)popup.Child).Child;

                    var actualItemOrder = listView.Items;
                    var expectedItemOrder = ((IEnumerable)autoSuggestBox.ItemsSource).Cast<object>().ToList();

                    if (!actualItemOrder.SequenceEqual(expectedItemOrder))
                    {
                        listView.ItemsSource = expectedItemOrder.ToList();
                    }

                    popup.VerticalOffset = autoSuggestBox.ActualHeight;
                }));
            }
            else if (suggestionListDirection == SuggestionListDirection.Up)
            {
                autoSuggestBox.SetValue(VerticalOffsetCallbackTokenProperty, popup.RegisterPropertyChangedCallback(Popup.VerticalOffsetProperty, (obj, dp) =>
                {
                    ListView listView = (ListView)((Border)popup.Child).Child;

                    var actualItemOrder = listView.Items;
                    var expectedItemOrder = ((IEnumerable)autoSuggestBox.ItemsSource).Cast<object>().Reverse().ToList();

                    if (!actualItemOrder.SequenceEqual(expectedItemOrder))
                    {
                        listView.ItemsSource = expectedItemOrder.ToList();
                    }

                    popup.VerticalOffset = -((Border)popup.Child).ActualHeight;
                }));
            }
        });
    }

    private static void SuggestionsContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var suggestionsContainer = ((Border)sender);
        suggestionsContainer.MinWidth = !double.IsNaN(suggestionsContainer.Width) ? suggestionsContainer.Width : 0;
        suggestionsContainer.Width = double.NaN;
    }

    private static FrameworkElement FindSuggestionsContainer(AutoSuggestBox autoSuggestBox)
    {
        var popup = FindSuggestionsPopup(autoSuggestBox);
        return (FrameworkElement)popup.Child;
    }

    private static Popup FindSuggestionsPopup(AutoSuggestBox autoSuggestBox)
    {
        autoSuggestBox.ApplyTemplate();
        return (Popup)autoSuggestBox.FindChild("SuggestionsPopup")!;
    }

    private static void WhenLoaded(AutoSuggestBox autoSuggestBox, Action<AutoSuggestBox> callback)
    {
        void AutoSuggestBox_Loaded(object sender, RoutedEventArgs e)
        {
            autoSuggestBox.Loaded -= AutoSuggestBox_Loaded;
            callback(autoSuggestBox);
        }

        if (!autoSuggestBox.IsLoaded)
        {
            autoSuggestBox.Loaded -= AutoSuggestBox_Loaded;
            autoSuggestBox.Loaded += AutoSuggestBox_Loaded;
        }

        if (autoSuggestBox.IsLoaded)
        {
            autoSuggestBox.Loaded -= AutoSuggestBox_Loaded;
            callback(autoSuggestBox);
        }
    }

}