using Essentials.NET.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public class ErrorReportService
{
    public async Task SendErrorReportAsync(string report)
    {
        string subject = $"{AppData.ApplicationName} Error Report {DateTime.Now:g}";
        await SendMailAsync(subject, report).ConfigureAwait(false);
        Log.Info("Error report sent successfully");
    }

    public async Task SendCrashReportAsync(string report)
    {
        string subject = $"{AppData.ApplicationName} Crash Report {DateTime.Now:g}";
        await SendMailAsync(subject, report).ConfigureAwait(false);
        Log.Info("Crash report sent successfully");
    }

    public async Task<string> CreateErrorReportAsync()
    {
        var logFilePath = Log.Logger.Appenders.OfType<FileAppender>().First().LogFilePath;
        var logFile = await StorageFile.GetFileFromPathAsync(logFilePath).AsTask().ConfigureAwait(false);
        string log = await FileIO.ReadTextAsync(logFile).AsTask().ConfigureAwait(false);
        return CreateReport(log);
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
        using var smtpClient = new SmtpClient("smtp.gmail.com", 587) { EnableSsl = true };
        smtpClient.Credentials = new NetworkCredential("universe.photos.app@gmail.com", CompileTimeConstants.GMailPassword);

        var emailMessage = new MailMessage();
        emailMessage.From = new MailAddress("universe.photos.app@gmail.com");
        emailMessage.To.Add("s.baeumlisberger@live.de");
        emailMessage.Subject = subject;
        emailMessage.Body = body;

        await smtpClient.SendMailAsync(emailMessage).ConfigureAwait(false);
    }

}
