using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.Utils;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(FlipViewPageModel))]
public sealed partial class FlipViewPage : Page, IMVVMControl<FlipViewPageModel>
{
    private FlipViewPageModel ViewModel => (FlipViewPageModel)DataContext;

    public FlipViewPage()
    {
        DataContext = PageModelFactory.CreateFlipViewPageModel(App.Current.Window.DialogService);
        this.InitializeMVVM();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnNavigatedTo(e.Parameter);
    }
}
