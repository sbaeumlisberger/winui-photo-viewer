using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels.Dialogs;
using Microsoft.UI.Xaml;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(DeleteSingleRawFilesDialogModel))]
public sealed partial class DeleteSingleRawFilesDialog : ContentDialog, IMVVMControl<DeleteSingleRawFilesDialogModel>
{

    private DeleteSingleRawFilesDialogModel ViewModel => (DeleteSingleRawFilesDialogModel)DataContext;

    public DeleteSingleRawFilesDialog()
    {
        this.InitializeComponentMVVM();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Progress?.Cancel();
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
