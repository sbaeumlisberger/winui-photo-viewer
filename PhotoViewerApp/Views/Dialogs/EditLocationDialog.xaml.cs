using Microsoft.UI.Xaml.Controls;
using PhotoViewerCore.ViewModels;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Utils;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(EditLocationDialogModel))]
public sealed partial class EditLocationDialog : ContentDialog, IMVVMControl<EditLocationDialogModel>
{
    private EditLocationDialogModel ViewModel => (EditLocationDialogModel)DataContext;

    public EditLocationDialog()
    {
        this.InitializeMVVM();
    }

}
