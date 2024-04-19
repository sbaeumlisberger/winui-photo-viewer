using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels;
using System.Linq;

namespace PhotoViewer.App.Views;
public sealed partial class MetadataPanel : UserControl, IMVVMControl<MetadataPanelModel>
{
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
            sender.ItemsSource = ViewModel!.PeopleSectionModel.FindSuggestions(sender.Text.Trim());
        }
    }

    private void PeopleAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var autoSuggestBox = (AutoSuggestBox)sender;
        if (autoSuggestBox.Text.Trim() == string.Empty)
        {
            autoSuggestBox.ItemsSource = ViewModel!.PeopleSectionModel.GetRecentSuggestions();
            autoSuggestBox.IsSuggestionListOpen = true;
        }
        else
        {
            autoSuggestBox.ItemsSource = ViewModel!.PeopleSectionModel.FindSuggestions(autoSuggestBox.Text.Trim());
        }
    }

    private void PeopleAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        sender.RunWhenTextChanged(args.QueryText, () => ViewModel!.PeopleSectionModel.AddPersonCommand.TryExecute());
    }

    private void KeywordAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = ViewModel!.KeywordsSectionModel.FindSuggestions(sender.Text.Trim());
        }
    }

    private void KeywordAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var autoSuggestBox = (AutoSuggestBox)sender;
        if (autoSuggestBox.Text.Trim() == string.Empty)
        {
            autoSuggestBox.ItemsSource = ViewModel!.KeywordsSectionModel.GetRecentSuggestions();
            autoSuggestBox.IsSuggestionListOpen = true;
        }
        else
        {
            autoSuggestBox.ItemsSource = ViewModel!.KeywordsSectionModel.FindSuggestions(autoSuggestBox.Text.Trim());
        }
    }

    private void KeywordAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        sender.RunWhenTextChanged(args.QueryText, () => ViewModel!.KeywordsSectionModel.AddKeywordCommand.TryExecute());
    }

    private void PeopleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel!.PeopleSectionModel.SelectedPeopleNames = ((ListView)sender).SelectedItems.Cast<ItemWithCountModel>().Select(x => x.Value).ToList();
    }
}
