using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(ComparePageModel))]
public sealed partial class ComparePage : Page, IMVVMControl<ComparePageModel>
{
    public ComparePage()
    {
        DataContext = App.Current.ViewModelFactory.CreateComparePageModel();
        this.InitializeComponentMVVM();
    }

    partial void DisconnectFromViewModel(ComparePageModel viewModel)
    {
        viewModel.Cleanup();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel!.OnNavigatedTo(e.Parameter);
    }
}
