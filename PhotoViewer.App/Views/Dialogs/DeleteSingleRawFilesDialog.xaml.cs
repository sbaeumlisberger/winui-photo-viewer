using Microsoft.UI.Xaml;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(DeleteSingleRawFilesDialogModel))]
public sealed partial class DeleteSingleRawFilesDialog : MultiViewDialogBase, IMVVMControl<DeleteSingleRawFilesDialogModel>
{
    public DeleteSingleRawFilesDialog()
    {
        this.InitializeComponentMVVM();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel!.Progress?.Cancel();
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
