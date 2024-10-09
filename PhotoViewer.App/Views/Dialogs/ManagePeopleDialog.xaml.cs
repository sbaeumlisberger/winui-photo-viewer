using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views.Dialogs;

[ViewRegistration(typeof(ManagePeopleDialogModel))]
public sealed partial class ManagePeopleDialog : ContentDialog, IMVVMControl<ManagePeopleDialogModel>
{
    public ManagePeopleDialog()
    {
        this.InitializeComponentMVVM();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        string name = (string)((FrameworkElement)sender).DataContext;
        ViewModel!.RemoveCommand.ExecuteAsync(name);
    }
}

