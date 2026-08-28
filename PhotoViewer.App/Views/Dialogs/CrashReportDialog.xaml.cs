using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.App.Views.Dialogs;

public sealed partial class CrashReportDialog : ContentDialog
{
    private readonly StorageFile report;

    public CrashReportDialog(StorageFile report)
    {
        this.report = report;
        this.InitializeComponent();
    }

    private async void ShowReportButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchFileAsync(report);
    }
}
