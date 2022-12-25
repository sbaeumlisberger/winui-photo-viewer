using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;

namespace PhotoViewerApp.Utils;

public static class AutoSuggestBoxExtension
{

    public static readonly DependencyProperty ShowSuggestionListWhenEmptyProperty = DependencyPropertyHelper.RegisterAttached(
       typeof(AutoSuggestBoxExtension), nameof(ShowSuggestionListWhenEmptyProperty), typeof(bool), false, OnShowSuggestionListWhenEmptyChanged);

    public static readonly DependencyProperty AllowPopupToOverflow = DependencyPropertyHelper.RegisterAttached(
       typeof(AutoSuggestBoxExtension), nameof(AllowPopupToOverflow), typeof(bool), false, OnAllowPopupToOverflowChanged);

    public static bool GetShowSuggestionListWhenEmpty(DependencyObject obj)
        => (bool)obj.GetValue(ShowSuggestionListWhenEmptyProperty);
    public static void SetShowSuggestionListWhenEmpty(DependencyObject obj, bool value) 
        => obj.SetValue(ShowSuggestionListWhenEmptyProperty, value);
    
    public static bool GetAllowPopupToOverflow(DependencyObject obj) => 
        (bool)obj.GetValue(AllowPopupToOverflow);
    public static void SetAllowPopupToOverflow(DependencyObject obj, bool value) =>
        obj.SetValue(AllowPopupToOverflow, value);

    private static void OnShowSuggestionListWhenEmptyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        AutoSuggestBox autoSuggestBox = (AutoSuggestBox)obj;

        if (e.NewValue is true)
        {
            autoSuggestBox.GotFocus += AutoSuggestBox_GotFocus;
        }
        else
        {
            autoSuggestBox.GotFocus -= AutoSuggestBox_GotFocus;
        }
    }

    private static async void OnAllowPopupToOverflowChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var autoSuggestBox = (AutoSuggestBox)obj;

        if (!autoSuggestBox.IsLoaded)
        {
            var tsc = new TaskCompletionSource();
            void AutoSuggestBox_Loaded(object sender, RoutedEventArgs e)
            {
                tsc.SetResult();
                autoSuggestBox.Loaded -= AutoSuggestBox_Loaded;
            }
            autoSuggestBox.Loaded += AutoSuggestBox_Loaded;
            if (autoSuggestBox.IsLoaded)
            {
                tsc.SetResult();
                autoSuggestBox.Loaded -= AutoSuggestBox_Loaded;
            }
            await tsc.Task;
        }

        var suggestionsContainer = (Border)((Popup)autoSuggestBox.FindChild("SuggestionsPopup")!).Child;

        if (e.NewValue is true)
        {
            suggestionsContainer.SizeChanged += SuggestionsContainer_SizeChanged;
        }
        else
        {
            suggestionsContainer.SizeChanged -= SuggestionsContainer_SizeChanged;
        }
    }


    private static void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        ((AutoSuggestBox)sender).IsSuggestionListOpen = true;
    }

    private static void SuggestionsContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var suggestionsContainer = ((Border)sender);
        suggestionsContainer.MinWidth = !double.IsNaN(suggestionsContainer.Width) ? suggestionsContainer.Width : 0;
        suggestionsContainer.Width = double.NaN;
    }

}