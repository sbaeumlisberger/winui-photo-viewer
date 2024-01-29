using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhotoViewer.App.Views.Dialogs;

public sealed partial class UnhandledExceptionDialog : ContentDialog
{
    public bool IsSendErrorReportChecked => sendErrorReportCheckBox.IsChecked ?? false;

    public UnhandledExceptionDialog(Window window, string exceptionMessage)
    {
        XamlRoot = window.Content.XamlRoot;
        RequestedTheme = ((FrameworkElement)window.Content).RequestedTheme;
        this.InitializeComponent();
        messageTextBlock.Text = "An unhandled exception occurred: " + exceptionMessage;
    }
}
