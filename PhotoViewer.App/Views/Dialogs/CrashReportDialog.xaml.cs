using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.App.Views.Dialogs;

public sealed partial class CrashReportDialog : ContentDialog
{
    private readonly string report;

    public CrashReportDialog(string report)
    {
        this.report = report;
        this.InitializeComponent();
    }

    private async void ShowReportButton_Click(object sender, RoutedEventArgs e)
    {
        var filPath = Path.Combine(Path.GetTempPath(), "universe-photos-crash-report.txt");
        File.WriteAllText(filPath, report);
        var storageFile = await StorageFile.GetFileFromPathAsync(filPath);
        await Launcher.LaunchFileAsync(storageFile);
    }
}
