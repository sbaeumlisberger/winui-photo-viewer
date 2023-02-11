using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PropertiesDialogModel))]
public sealed partial class PropertiesDialog : ContentDialog, IMVVMControl<PropertiesDialogModel>
{

    private PropertiesDialogModel ViewModel => (PropertiesDialogModel)DataContext;

    public PropertiesDialog()
    {
        this.InitializeComponentMVVM();
    }

}
