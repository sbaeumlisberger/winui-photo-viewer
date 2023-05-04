using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels;
using System;

namespace PhotoViewer.App.Views;
public sealed partial class MetadataPanel : UserControl, IMVVMControl<MetadataPanelModel>
{
    public MetadataPanelModel ViewModel => (MetadataPanelModel)DataContext;

    public MetadataPanel()
    {
        this.InitializeComponentMVVM();
    }

    private string ResolvePlaceholder(bool hasDifferentValues, string placeholder)
    {
        return hasDifferentValues ? Strings.MetadataPanel_DifferentValuesPlaceholder : placeholder;
    }

    private void PeopleAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = ViewModel.PeopleSectionModel.FindSuggestions(sender.Text);
        }
    }

    private void PeopleAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var autoSuggestBox = (AutoSuggestBox)sender;
        if (autoSuggestBox.Text == string.Empty)
        {
            autoSuggestBox.ItemsSource = ViewModel.PeopleSectionModel.GetRecentSuggestions();
            autoSuggestBox.IsSuggestionListOpen = true;
        }
        else 
        {
            autoSuggestBox.ItemsSource = ViewModel.PeopleSectionModel.FindSuggestions(autoSuggestBox.Text);
        }
    }

    private void KeywordAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = ViewModel.KeywordsSectionModel.FindSuggestions(sender.Text);
        }
    }

    private void KeywordAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var autoSuggestBox = (AutoSuggestBox)sender;
        if (autoSuggestBox.Text == string.Empty)
        {
            autoSuggestBox.ItemsSource = ViewModel.KeywordsSectionModel.GetRecentSuggestions();
            autoSuggestBox.IsSuggestionListOpen = true;
        }
        else
        {
            autoSuggestBox.ItemsSource = ViewModel.KeywordsSectionModel.FindSuggestions(autoSuggestBox.Text);
        }
    }

}
