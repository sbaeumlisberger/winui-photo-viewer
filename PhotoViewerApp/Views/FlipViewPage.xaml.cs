using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(FlipViewPageModel))]
public sealed partial class FlipViewPage : Page
{
    private FlipViewPageModel ViewModel => (FlipViewPageModel)DataContext;

    public FlipViewPage()
    {
        DataContext = PageModelFactory.CreateFlipViewPageModel(App.Current.Window.DialogService);
        this.InitializeMVVM<FlipViewPageModel>(InitializeComponent,
            connectToViewModel: (viewModel) => Bindings.Initialize(),
            disconnectFromViewModel: (viewModel) => Bindings.StopTracking());
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnNavigatedTo(e.Parameter);
    }
}
