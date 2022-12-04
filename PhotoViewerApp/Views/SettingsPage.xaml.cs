using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Resources;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.Models;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(SettingsPageModel))]
public sealed partial class SettingsPage : Page
{
    private SettingsPageModel ViewModel { get; } = PageModelFactory.CreateSettingsPageModel(App.Current.Window.DialogService);

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        App.Current.Window.Title = Strings.SettingsPage_Title;
    }

    private void DeleteLinkedFilesOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is DeleteLinkedFilesOption option)
        {
            ViewModel.Settings.DeleteLinkedFilesOption = option;
        }
    }
}
