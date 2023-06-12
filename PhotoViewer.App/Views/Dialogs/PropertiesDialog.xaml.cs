using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PropertiesDialogModel))]
public sealed partial class PropertiesDialog : ContentDialog, IMVVMControl<PropertiesDialogModel>
{
    public PropertiesDialog()
    {
        this.InitializeComponentMVVM();
    }

}
