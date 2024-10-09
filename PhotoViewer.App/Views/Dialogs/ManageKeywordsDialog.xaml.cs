using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views.Dialogs;

[ViewRegistration(typeof(ManageKeywordsDialogModel))]
public sealed partial class ManageKeywordsDialog : ContentDialog, IMVVMControl<ManageKeywordsDialogModel>
{
    public ManageKeywordsDialog()
    {
        this.InitializeComponentMVVM();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        string keyword = (string)((FrameworkElement)sender).DataContext;
        ViewModel!.RemoveCommand.ExecuteAsync(keyword);
    }
}
