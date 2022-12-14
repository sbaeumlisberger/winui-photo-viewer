using Microsoft.UI.Xaml.Controls;
using PhotoViewerCore.ViewModels;
using PhotoViewerApp.Utils;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(PropertiesDialogModel))]
public sealed partial class PropertiesDialog : ContentDialog
{

    private PropertiesDialogModel ViewModel => (PropertiesDialogModel)DataContext;

    public PropertiesDialog()
    {
        this.InitializeMVVM<PropertiesDialogModel>(InitializeComponent,
            connectToViewModel: (viewModel) => Bindings.Initialize(),
            disconnectFromViewModel: (viewModel) => Bindings.StopTracking());
    }

}
