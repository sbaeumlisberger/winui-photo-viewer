using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Resources;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(SettingsPageModel))]
public sealed partial class SettingsPage : Page
{
    private SettingsPageModel ViewModel { get; } = PageModelFactory.CreateSettingsPageModel(App.Current.Window.DialogService);

    public SettingsPage()
    {
        this.InitializeComponent();
        App.Current.Window.Title = Strings.SettingsPage_Title;
    }
}
