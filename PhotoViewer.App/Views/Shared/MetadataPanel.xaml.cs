using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Controls;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
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

    private void PeopleAutoSuggestBox_SuggestionsRequested(AutoSuggestionBox sender, EventArgs args)
    {
        sender.ItemsSource = ViewModel!.PeopleSectionModel.FindSuggestions(sender.Text.Trim());
    }

    private void KeywordAutoSuggestBox_SuggestionsRequested(AutoSuggestionBox sender, EventArgs args)
    {
        sender.ItemsSource = ViewModel!.KeywordsSectionModel.FindSuggestions(sender.Text.Trim());
    }

    private void PeopleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel!.PeopleSectionModel.SelectedPeopleNames = ((ListView)sender).SelectedItems.Cast<ItemWithCountModel>().Select(x => x.Value).ToList();
    }
}
