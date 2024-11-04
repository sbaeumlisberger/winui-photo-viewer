using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.App.Views.Dialogs;

public enum UnhandledExceptionDialogResult { Exit, Ignore }

public sealed partial class UnhandledExceptionDialog : UserControl
{
    public bool IsSendErrorReportChecked { get; set; } = !Debugger.IsAttached;

    private readonly string report;

    private TaskCompletionSource<UnhandledExceptionDialogResult>? tcs;

    public UnhandledExceptionDialog(string exceptionMessage, string report)
    {
        this.report = report;

        this.InitializeComponent();

        messageTextBlock.Text = "An error occurred: " + exceptionMessage;
    }

    public async Task<UnhandledExceptionDialogResult> GetResultAsync()
    {
        tcs = new TaskCompletionSource<UnhandledExceptionDialogResult>();
        return await tcs.Task;
    }

    private async void ShowReportButton_Click(object sender, RoutedEventArgs e)
    {
        var filPath = Path.Combine(Path.GetTempPath(), "universe-photos-error-report.txt");
        File.WriteAllText(filPath, report);
        var storageFile = await StorageFile.GetFileFromPathAsync(filPath);
        await Launcher.LaunchFileAsync(storageFile);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        tcs?.SetResult(UnhandledExceptionDialogResult.Exit);
    }

    private void IgnoreButton_Click(object sender, RoutedEventArgs e)
    {
        tcs?.SetResult(UnhandledExceptionDialogResult.Ignore);
    }
}
