using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(PrefixFilesByDateDialogModel))]
public sealed partial class PrefixFilesByDateDialog : MultiViewDialogBase, IMVVMControl<PrefixFilesByDateDialogModel>
{
    public PrefixFilesByDateDialog()
    {
        this.InitializeComponentMVVM();
    }

}
