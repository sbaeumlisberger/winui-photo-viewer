using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.App.Views.Dialogs;

public sealed partial class UnhandledExceptionDialog : ContentDialog
{
    public bool IsSendErrorReportChecked { get; set; } = !Debugger.IsAttached;

    private readonly string report;

    public UnhandledExceptionDialog(string exceptionMessage, string report)
    {
        this.report = report;
        this.InitializeComponent();
        messageTextBlock.Text = "An error occurred: " + exceptionMessage;
    }

    private async void ShowReportButton_Click(object sender, RoutedEventArgs e)
    {
        var filPath = Path.Combine(Path.GetTempPath(), "universe-photos-error-report.txt");
        File.WriteAllText(filPath, report);
        var storageFile = await StorageFile.GetFileFromPathAsync(filPath);
        await Launcher.LaunchFileAsync(storageFile);
    }
}
