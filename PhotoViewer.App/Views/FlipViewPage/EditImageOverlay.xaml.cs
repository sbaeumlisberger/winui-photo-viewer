using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class EditImageOverlay : UserControl
{
    public EditImageOverlayModel ViewModel => (EditImageOverlayModel)DataContext;

    private bool closingConfirmed = false;

    public EditImageOverlay()
    {
        this.InitializeComponent();

        App.Current.Window.Closing += Window_Closing;
    }

    private async void Window_Closing(MainWindow sender, AppWindowClosingEventArgs args)
    {
        if (closingConfirmed)
        {
            return;
        }

        args.Cancel = true;

        if (await ViewModel.ConfirmCloseAsync())
        {
            closingConfirmed = true;
            App.Current.Window.Close();
        }
    }
}
