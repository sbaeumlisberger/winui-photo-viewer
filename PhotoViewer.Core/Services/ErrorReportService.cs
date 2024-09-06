using Essentials.NET.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public class ErrorReportService
{
    public async Task TrySendErrorReportAsync()
    {
        try
        {
            var logFilePath = Log.Logger.Appenders.OfType<FileAppender>().First().LogFilePath;
            var logFile = await StorageFile.GetFileFromPathAsync(logFilePath).AsTask().ConfigureAwait(false);
            string log = await FileIO.ReadTextAsync(logFile).AsTask().ConfigureAwait(false);

            string subject = $"WinUI Photo Viewer Error Report {DateTime.Now:g}";
            string body = CreateReport(log);

            await SendMailAsync(subject, body).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to send error report: " + ex);
        }
    }

    public async Task SendCrashReportAsync(string report)
    {
        string subject = $"WinUI Photo Viewer Crash Report {DateTime.Now:g}";
        await SendMailAsync(subject, report).ConfigureAwait(false);
    }

    public string CreateReport(string message)
    {
        var version = Package.Current.Id.Version;
        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine("App Version: " + version.Major + "." + version.Minor + "." + version.Build);
        bodyBuilder.AppendLine("OS Version: " + Environment.OSVersion.VersionString);
        bodyBuilder.AppendLine();
        bodyBuilder.AppendLine(message);
        return bodyBuilder.ToString();
    }

    private async Task SendMailAsync(string subject, string body)
    {
        using var smtpClient = new SmtpClient("smtp-mail.outlook.com", 587) { EnableSsl = true };
        smtpClient.Credentials = new NetworkCredential("universe-photos@outlook.com", CompileTimeConstants.EMailPassword);

        var emailMessage = new MailMessage();
        emailMessage.From = new MailAddress("universe-photos@outlook.com");
        emailMessage.To.Add("s.baeumlisberger@live.de");
        emailMessage.Subject = subject;
        emailMessage.Body = body;

        await smtpClient.SendMailAsync(emailMessage).ConfigureAwait(false);
    }

}
