using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels.Dialogs;

namespace PhotoViewer.App.Views.Dialogs;

[ViewRegistration(typeof(ShiftDatenTakenDialogModel))]
public sealed partial class ShiftDatenTakenDialog : ContentDialog, IMVVMControl<ShiftDatenTakenDialogModel>
{
    private ShiftDatenTakenDialogModel ViewModel => (ShiftDatenTakenDialogModel)DataContext;

    public ShiftDatenTakenDialog()
    {
        this.InitializeComponentMVVM();
    }

}
