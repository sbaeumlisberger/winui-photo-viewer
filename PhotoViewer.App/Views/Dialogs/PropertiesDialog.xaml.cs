using Microsoft.UI.Xaml.Controls;
using PhotoViewerCore.ViewModels;
using PhotoViewer.App.Utils;
using PhotoViewerCore.Utils;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PropertiesDialogModel))]
public sealed partial class PropertiesDialog : ContentDialog, IMVVMControl<PropertiesDialogModel>
{

    private PropertiesDialogModel ViewModel => (PropertiesDialogModel)DataContext;

    public PropertiesDialog()
    {
        this.InitializeMVVM();
    }

}
