using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections;
using System.Linq;

namespace PhotoViewer.App.Utils;

public enum SuggestionListDirection
{
    Auto,
    Down,
    Up
}

public class AutoSuggestBoxExtension
{
    public static readonly DependencyProperty IsSuggestionListOverflowEnabledProperty = DependencyPropertyHelper<AutoSuggestBoxExtension>
        .RegisterAttached(false, OnIsSuggestionListOverflowEnabledChanged);

    public static readonly DependencyProperty SuggestionListDirectionProperty = DependencyPropertyHelper<AutoSuggestBoxExtension>
        .RegisterAttached(SuggestionListDirection.Auto, OnSuggestionListDirectionChanged);

    private static readonly DependencyProperty IsSuggestionListOpenChangedCallbackTokenProperty = DependencyPropertyHelper<AutoSuggestBoxExtension>
        .RegisterAttached<long?>(null);

    private static readonly DependencyProperty VerticalOffsetChangedCallbackTokenProperty = DependencyPropertyHelper<AutoSuggestBoxExtension>
        .RegisterAttached<long?>(null);

    public static bool GetIsSuggestionListOverflowEnabled(DependencyObject obj) => (bool)obj.GetValue(IsSuggestionListOverflowEnabledProperty);
    public static void SetIsSuggestionListOverflowEnabled(DependencyObject obj, bool value) => obj.SetValue(IsSuggestionListOverflowEnabledProperty, value);

    public static SuggestionListDirection GetSuggestionListDirection(DependencyObject obj) => (SuggestionListDirection)obj.GetValue(SuggestionListDirectionProperty);
    public static void SetSuggestionListDirection(DependencyObject obj, SuggestionListDirection value) => obj.SetValue(SuggestionListDirectionProperty, value);

    private static void OnIsSuggestionListOverflowEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        WhenSuggestionListOpened((AutoSuggestBox)obj, (autoSuggestBox, popup) =>
        {
            var suggestionsContainer = (FrameworkElement)popup.Child;

            if (GetIsSuggestionListOverflowEnabled(autoSuggestBox))
            {
                suggestionsContainer.SizeChanged += onSuggestionsContainerSizeChanged;
            }
            else
            {
                suggestionsContainer.SizeChanged -= onSuggestionsContainerSizeChanged;
            }

            static void onSuggestionsContainerSizeChanged(object sender, SizeChangedEventArgs e)
            {
                var suggestionsContainer = (Border)sender;
                suggestionsContainer.MinWidth = !double.IsNaN(suggestionsContainer.Width) ? suggestionsContainer.Width : 0;
                suggestionsContainer.Width = double.NaN;
            }
        });
    }

    private static void OnSuggestionListDirectionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        WhenSuggestionListOpened((AutoSuggestBox)obj, (autoSuggestBox, popup) =>
        {
            var suggestionsContainer = (FrameworkElement)popup.Child;

            var suggestionListDirection = GetSuggestionListDirection(autoSuggestBox);

            if (autoSuggestBox.GetValue(VerticalOffsetChangedCallbackTokenProperty) is long verticalOffsetCallbackToken)
            {
                popup.UnregisterPropertyChangedCallback(Popup.VerticalOffsetProperty, verticalOffsetCallbackToken);
            }

            suggestionsContainer.SizeChanged -= onSuggestionsContainerSizeChanged;

            if (suggestionListDirection != SuggestionListDirection.Auto)
            {
                var token = popup.RegisterPropertyChangedCallback(Popup.VerticalOffsetProperty, (obj, dp) =>
                {
                    update();
                });
                autoSuggestBox.SetValue(VerticalOffsetChangedCallbackTokenProperty, token);

                suggestionsContainer.SizeChanged += onSuggestionsContainerSizeChanged;

                update();
            }

            void onSuggestionsContainerSizeChanged(object sender, SizeChangedEventArgs e)
            {
                update();
            }

            void update()
            {
                ListView listView = (ListView)((Border)popup.Child).Child;

                var actualItemOrder = listView.Items;

                if (autoSuggestBox.ItemsSource is IEnumerable itemSource)
                {
                    if (suggestionListDirection == SuggestionListDirection.Down)
                    {
                        var expectedItemOrder = itemSource.Cast<object>().ToList();

                        if (!actualItemOrder.SequenceEqual(expectedItemOrder))
                        {
                            listView.ItemsSource = expectedItemOrder.ToList();
                        }

                        popup.VerticalOffset = autoSuggestBox.ActualHeight;
                    }
                    else if (suggestionListDirection == SuggestionListDirection.Up)
                    {
                        var expectedItemOrder = itemSource.Cast<object>().Reverse().ToList();

                        if (!actualItemOrder.SequenceEqual(expectedItemOrder))
                        {
                            listView.ItemsSource = expectedItemOrder.ToList();
                        }

                        popup.VerticalOffset = -((Border)popup.Child).ActualHeight;
                    }
                }
            }
        });
    }

    private static void WhenSuggestionListOpened(AutoSuggestBox autoSuggestBox, Action<AutoSuggestBox, Popup> callback)
    {
        if (autoSuggestBox.GetValue(IsSuggestionListOpenChangedCallbackTokenProperty) is long token)
        {
            autoSuggestBox.UnregisterPropertyChangedCallback(AutoSuggestBox.IsSuggestionListOpenProperty, token);
        }

        token = autoSuggestBox.RegisterPropertyChangedCallbackSafely(AutoSuggestBox.IsSuggestionListOpenProperty, isSuggestionListOpenPropertyChangedCallback);
        autoSuggestBox.SetValue(IsSuggestionListOpenChangedCallbackTokenProperty, token);

        void isSuggestionListOpenPropertyChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (autoSuggestBox.GetValue(IsSuggestionListOpenChangedCallbackTokenProperty) is long token)
            {
                autoSuggestBox.UnregisterPropertyChangedCallback(AutoSuggestBox.IsSuggestionListOpenProperty, token);
                autoSuggestBox.SetValue(IsSuggestionListOpenChangedCallbackTokenProperty, null);
            }
            callback(autoSuggestBox, FindSuggestionsPopup(autoSuggestBox));
        }

        if (autoSuggestBox.IsSuggestionListOpen)
        {
            callback(autoSuggestBox, FindSuggestionsPopup(autoSuggestBox));
        }
    }

    private static Popup FindSuggestionsPopup(AutoSuggestBox autoSuggestBox)
    {
        return (Popup)autoSuggestBox.FindChild("SuggestionsPopup")!;
    }

}