using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.App.Views.Dialogs;

public enum UnhandledExceptionDialogResult { Exit, Ignore }

public sealed partial class UnhandledExceptionDialog : UserControl
{
    public bool IsSendErrorReportChecked { get; set; } = !Debugger.IsAttached;

    private readonly StorageFile report;

    private TaskCompletionSource<UnhandledExceptionDialogResult>? tcs;

    public UnhandledExceptionDialog(string exceptionMessage, StorageFile report)
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
        await Launcher.LaunchFileAsync(report);
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
