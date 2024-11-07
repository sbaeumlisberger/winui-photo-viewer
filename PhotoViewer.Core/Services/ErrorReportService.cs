using Essentials.NET.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public class ErrorReportService
{
    private readonly PackageVersion appVersion;

    private readonly IEventLogService eventLogService;

    public ErrorReportService(PackageVersion appVersion, IEventLogService eventLogService)
    {
        this.appVersion = appVersion;
        this.eventLogService = eventLogService;
    }

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

    public async Task<string?> CreateCrashReportAsync()
    {
        var errors = eventLogService.GetErrorsSinceLastCheck();

        if (errors.Count == 0)
        {
            return null;
        }

        string log;

        if (FindLogFileFromCrash() is string logFileFromCrash)
        {
            log = await File.ReadAllTextAsync(logFileFromCrash);
        }
        else
        {
            log = "no log file found";
        }

        string reportBody = string.Join("\n\n", [.. errors, log]);

        return CreateReport(reportBody);
    }

    private string CreateReport(string body)
    {
        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine("App Version: " + appVersion.Major + "." + appVersion.Minor + "." + appVersion.Build);
        bodyBuilder.AppendLine("OS Version: " + Environment.OSVersion.VersionString);
        bodyBuilder.AppendLine();
        bodyBuilder.AppendLine(body);
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

    private string? FindLogFileFromCrash()
    {
        var fileAppender = Log.Logger.Appenders.OfType<FileAppender>().First();
        string[] nonArchivedLogsFiles = Directory.GetFiles(fileAppender.LogFolderPath, "*.txt");

        string? logFileFromCrash = nonArchivedLogsFiles
            .OrderByDescending(filePath => filePath)
            .SkipWhile(filePath => filePath != fileAppender.LogFilePath)
            .FirstOrDefault(filePath => !IsFileInUse(filePath));

        return logFileFromCrash;

        static bool IsFileInUse(string filePath)
        {
            try
            {
                using (File.OpenWrite(filePath))
                {
                    return false;
                }
            }
            catch (Exception e) when (e.HResult == unchecked((int)0x80070020))
            {
                return true;
            }
        }
    }

}
