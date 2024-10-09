using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Linq;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(SettingsPageModel))]
public sealed partial class SettingsPage : Page, IMVVMControl<SettingsPageModel>
{
    public SettingsPage()
    {
        DataContext = App.Current.ViewModelFactory.CreateSettingsPageModel();
        this.InitializeComponentMVVM();
    }

    partial void DisconnectFromViewModel(SettingsPageModel viewModel)
    {
        viewModel.Cleanup();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel!.OnNavigatedTo();
    }

    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is AppTheme theme)
        {
            ViewModel!.Settings.Theme = theme;
        }
    }

    private void DeleteLinkedFilesOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is DeleteLinkedFilesOption option)
        {
            ViewModel!.Settings.DeleteLinkedFilesOption = option;
        }
    }
}
