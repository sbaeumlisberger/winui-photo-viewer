using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views.Dialogs;

[ViewRegistration(typeof(ShiftDateTakenDialogModel))]
public sealed partial class ShiftDateTakenDialog : MultiViewDialogBase, IMVVMControl<ShiftDateTakenDialogModel>
{
    public ShiftDateTakenDialog()
    {
        this.InitializeComponentMVVM();
    }

}
