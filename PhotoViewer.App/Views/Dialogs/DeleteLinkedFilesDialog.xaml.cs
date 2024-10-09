using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(DeleteLinkedFilesDialogModel))]
public sealed partial class DeleteLinkedFilesDialog : ContentDialog, IMVVMControl<DeleteLinkedFilesDialogModel>
{
    public DeleteLinkedFilesDialog()
    {
        this.InitializeComponentMVVM();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel!.IsYes = true;
    }
}
