using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.ViewModels.Dialogs;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(MoveRawFilesToSubfolderDialogModel))]
public sealed partial class MoveRawFilesToSubfolderDialog : MultiViewDialogBase, IMVVMControl<MoveRawFilesToSubfolderDialogModel>
{
    public MoveRawFilesToSubfolderDialog()
    {
        this.InitializeComponentMVVM();
    }

}
