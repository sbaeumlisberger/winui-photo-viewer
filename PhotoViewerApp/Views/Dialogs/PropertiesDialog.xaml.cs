using Microsoft.UI.Xaml.Controls;
using PhotoViewerCore.ViewModels;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Utils;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(PropertiesDialogModel))]
public sealed partial class PropertiesDialog : ContentDialog, IMVVMControl<PropertiesDialogModel>
{

    private PropertiesDialogModel ViewModel => (PropertiesDialogModel)DataContext;

    public PropertiesDialog()
    {
        this.InitializeMVVM();
    }

}
