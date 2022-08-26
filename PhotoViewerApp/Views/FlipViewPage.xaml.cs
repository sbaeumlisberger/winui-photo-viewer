using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(FlipViewPageModel))]
public sealed partial class FlipViewPage : Page
{
    private FlipViewPageModel ViewModel { get; } = PageModelFactory.CreateFlipViewPageModel(App.Current.Window.DialogService);

    public FlipViewPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnNavigatedTo(e.Parameter);
    }
}
