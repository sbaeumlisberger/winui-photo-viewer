using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PropertiesDialogModel))]
public sealed partial class PropertiesDialog : ContentDialog, IMVVMControl<PropertiesDialogModel>
{
    public PropertiesDialog()
    {
        this.InitializeComponentMVVM();
    }

}
