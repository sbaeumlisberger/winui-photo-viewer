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
    public static readonly DependencyProperty IsSuggestionListOverflowEnabledProperty = DependencyPropertyHelper.RegisterAttached(
       typeof(AutoSuggestBoxExtension), nameof(IsSuggestionListOverflowEnabledProperty), false, OnIsSuggestionListOverflowEnabledChanged);

    public static readonly DependencyProperty SuggestionListDirectionProperty = DependencyPropertyHelper.RegisterAttached(
       typeof(AutoSuggestBoxExtension), nameof(SuggestionListDirectionProperty), SuggestionListDirection.Auto, OnSuggestionListDirectionChanged);

    private static readonly DependencyProperty IsSuggestionListOpenChangedCallbackTokenProperty = DependencyPropertyHelper.RegisterAttached<long?>
    (typeof(AutoSuggestBoxExtension), nameof(IsSuggestionListOpenChangedCallbackTokenProperty), null);

    private static readonly DependencyProperty VerticalOffsetChangedCallbackTokenProperty = DependencyPropertyHelper.RegisterAttached<long?>
        (typeof(AutoSuggestBoxExtension), nameof(VerticalOffsetChangedCallbackTokenProperty), null);

    public static bool GetIsSuggestionListOverflowEnabled(DependencyObject obj) => (bool)obj.GetValue(IsSuggestionListOverflowEnabledProperty);
    public static void SetIsSuggestionListOverflowEnabled(DependencyObject obj, bool value) => obj.SetValue(IsSuggestionListOverflowEnabledProperty, value);

    public static SuggestionListDirection GetSuggestionListDirection(DependencyObject obj) => (SuggestionListDirection)obj.GetValue(SuggestionListDirectionProperty);
    public static void SetSuggestionListDirection(DependencyObject obj, SuggestionListDirection value) => obj.SetValue(SuggestionListDirectionProperty, value);

    private static void OnIsSuggestionListOverflowEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        WhenSuggestionListOpened((AutoSuggestBox)obj, autoSuggestBox =>
        {
            var suggestionsContainer = FindSuggestionsContainer(autoSuggestBox);

            if (GetIsSuggestionListOverflowEnabled(autoSuggestBox))
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
        WhenSuggestionListOpened((AutoSuggestBox)obj, autoSuggestBox =>
        {
            var suggestionListDirection = GetSuggestionListDirection(autoSuggestBox);

            var popup = FindSuggestionsPopup(autoSuggestBox);

            if (autoSuggestBox.GetValue(VerticalOffsetChangedCallbackTokenProperty) is long verticalOffsetCallbackToken)
            {
                popup.UnregisterPropertyChangedCallback(Popup.VerticalOffsetProperty, verticalOffsetCallbackToken);
            }

            if (suggestionListDirection == SuggestionListDirection.Down)
            {
                autoSuggestBox.SetValue(VerticalOffsetChangedCallbackTokenProperty, popup.RegisterPropertyChangedCallback(Popup.VerticalOffsetProperty, (obj, dp) =>
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
                autoSuggestBox.SetValue(VerticalOffsetChangedCallbackTokenProperty, popup.RegisterPropertyChangedCallback(Popup.VerticalOffsetProperty, (obj, dp) =>
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
        var suggestionsContainer = (Border)sender;
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
        return (Popup)autoSuggestBox.FindChild("SuggestionsPopup")!;
    }

    private static void WhenSuggestionListOpened(AutoSuggestBox autoSuggestBox, Action<AutoSuggestBox> callback)
    {
        if (autoSuggestBox.GetValue(IsSuggestionListOpenChangedCallbackTokenProperty) is long token)
        {
            autoSuggestBox.UnregisterPropertyChangedCallback(AutoSuggestBox.IsSuggestionListOpenProperty, token);
        }

        token = default;
        token = autoSuggestBox.RegisterPropertyChangedCallbackSafely(AutoSuggestBox.IsSuggestionListOpenProperty, isSuggestionListOpenPropertyChangedCallback);
        autoSuggestBox.SetValue(IsSuggestionListOpenChangedCallbackTokenProperty, token); 

        void isSuggestionListOpenPropertyChangedCallback(DependencyObject sender, DependencyProperty dp) 
        {
            autoSuggestBox.UnregisterPropertyChangedCallback(AutoSuggestBox.IsSuggestionListOpenProperty, token);
            autoSuggestBox.SetValue(IsSuggestionListOpenChangedCallbackTokenProperty, null);
            callback(autoSuggestBox);
        }
    }

}