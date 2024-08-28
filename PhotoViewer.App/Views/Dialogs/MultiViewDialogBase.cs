using Microsoft.UI.Xaml.Controls;
namespace PhotoViewer.App.Views.Dialogs;

public partial class MultiViewDialogBase : ContentDialog
{
    private bool closeRequested;

    public MultiViewDialogBase()
    {
        Closing += DialogBase_Closing;
    }

    public void Close()
    {
        closeRequested = true;
        Hide();
    }

    private void DialogBase_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        args.Cancel = !closeRequested;
        closeRequested = false;
    }
}
