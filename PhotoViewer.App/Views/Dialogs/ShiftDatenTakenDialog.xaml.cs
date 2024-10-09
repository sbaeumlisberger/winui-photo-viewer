using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views.Dialogs;

[ViewRegistration(typeof(ShiftDatenTakenDialogModel))]
public sealed partial class ShiftDatenTakenDialog : MultiViewDialogBase, IMVVMControl<ShiftDatenTakenDialogModel>
{
    public ShiftDatenTakenDialog()
    {
        this.InitializeComponentMVVM();
    }

}
