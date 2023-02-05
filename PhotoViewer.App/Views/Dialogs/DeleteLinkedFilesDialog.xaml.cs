using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Utils;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(DeleteLinkedFilesDialogModel))]
public sealed partial class DeleteLinkedFilesDialog : ContentDialog
{

    private DeleteLinkedFilesDialogModel ViewModel => (DeleteLinkedFilesDialogModel)DataContext;

    public DeleteLinkedFilesDialog()
    {
        InitializeComponent();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.IsYes = true;
    }
}
