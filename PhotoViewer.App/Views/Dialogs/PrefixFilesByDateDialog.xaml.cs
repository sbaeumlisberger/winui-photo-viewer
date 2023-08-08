using PhotoViewer.App.Utils;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.ViewModels.Dialogs;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PrefixFilesByDateDialogModel))]
public sealed partial class PrefixFilesByDateDialog : MultiViewDialogBase, IMVVMControl<PrefixFilesByDateDialogModel>
{
    public PrefixFilesByDateDialog()
    {
        this.InitializeComponentMVVM();
    }

}
