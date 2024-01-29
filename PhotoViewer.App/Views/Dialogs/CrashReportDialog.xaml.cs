using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhotoViewer.App.Views.Dialogs;

public sealed partial class CrashReportDialog : ContentDialog
{
    public CrashReportDialog(Window window)
    {
        XamlRoot = window.Content.XamlRoot;
        RequestedTheme = ((FrameworkElement)window.Content).RequestedTheme;
        this.InitializeComponent();
    }
}
