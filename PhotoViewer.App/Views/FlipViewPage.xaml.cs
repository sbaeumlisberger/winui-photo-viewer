using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(FlipViewPageModel))]
public sealed partial class FlipViewPage : Page, IMVVMControl<FlipViewPageModel>
{
    private FlipViewPageModel ViewModel => (FlipViewPageModel)DataContext;

    public FlipViewPage()
    {
        DataContext = ViewModelFactory.Instance.CreateFlipViewPageModel();
        this.InitializeMVVM();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnNavigatedTo(e.Parameter);
    }
}
