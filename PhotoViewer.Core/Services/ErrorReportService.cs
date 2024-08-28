using Essentials.NET.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public class ErrorReportService
{
    public async Task SendErrorReportAsync()
    {
        var logFile = await StorageFile.GetFileFromPathAsync(Log.Logger.Appenders.OfType<FileAppender>().First().LogFilePath).AsTask().ConfigureAwait(false);
        string log = await FileIO.ReadTextAsync(logFile).AsTask().ConfigureAwait(false);
  
        string subject = $"WinUI Photo Viewer Error Report {DateTime.Now:g}";
        string body = CreateBody(log);

        await SendMailAsync(subject, body).ConfigureAwait(false);
    }

    public async Task SendCrashReportAsync(string crashInfo)
    {
        string subject = $"WinUI Photo Viewer Crash Report {DateTime.Now:g}";
        string body = CreateBody(crashInfo);

        await SendMailAsync(subject, body).ConfigureAwait(false);
    }

    private string CreateBody(string message)
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
