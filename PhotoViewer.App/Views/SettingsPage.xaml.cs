using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Linq;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(SettingsPageModel))]
public sealed partial class SettingsPage : Page, IMVVMControl<SettingsPageModel>
{
    private SettingsPageModel ViewModel => (SettingsPageModel)DataContext;

    public SettingsPage()
    {
        DataContext = ViewModelFactory.Instance.CreateSettingsPageModel();
        this.InitializeComponentMVVM();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        App.Current.Window.Title = Strings.SettingsPage_Title + " - WinUI Photo Viewer"; // TODO use message
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.Cleanup();
    }

    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is AppTheme theme)
        {
            ViewModel.Settings.Theme = theme;
        }
    }

    private void DeleteLinkedFilesOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is DeleteLinkedFilesOption option)
        {
            ViewModel.Settings.DeleteLinkedFilesOption = option;
        }
    }
}
