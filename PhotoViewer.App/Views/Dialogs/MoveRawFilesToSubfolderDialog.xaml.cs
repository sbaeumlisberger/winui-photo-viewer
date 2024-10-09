using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(MoveRawFilesToSubfolderDialogModel))]
public sealed partial class MoveRawFilesToSubfolderDialog : MultiViewDialogBase, IMVVMControl<MoveRawFilesToSubfolderDialogModel>
{
    public MoveRawFilesToSubfolderDialog()
    {
        this.InitializeComponentMVVM();
    }

}
