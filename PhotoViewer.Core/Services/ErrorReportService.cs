using Essentials.NET.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public class ErrorReportService
{
    private readonly string applicationName;

    private readonly PackageVersion appVersion;

    private readonly IEventLogService eventLogService;

    public ErrorReportService(string applicationName, PackageVersion appVersion, IEventLogService eventLogService)
    {
        this.applicationName = applicationName;
        this.appVersion = appVersion;
        this.eventLogService = eventLogService;
    }

    public async Task SendErrorReportAsync(StorageFile reportFile)
    {
        string report = await FileIO.ReadTextAsync(reportFile);
        string subject = $"{applicationName} Error Report {DateTime.Now:g}";
        await SendMailAsync(subject, report).ConfigureAwait(false);
        Log.Info("Error report sent successfully");
    }

    public async Task SendCrashReportAsync(StorageFile reportFile)
    {
        string report = await FileIO.ReadTextAsync(reportFile);
        string subject = $"{applicationName} Crash Report {DateTime.Now:g}";
        await SendMailAsync(subject, report).ConfigureAwait(false);
        Log.Info("Crash report sent successfully");
    }

    public async Task<StorageFile> CreateErrorReportAsync()
    {
        var logFilePath = Log.Logger.Appenders.OfType<FileAppender>().First().LogFilePath;
        var logFile = await StorageFile.GetFileFromPathAsync(logFilePath).AsTask().ConfigureAwait(false);
        string log = await FileIO.ReadTextAsync(logFile).AsTask().ConfigureAwait(false);
        return await CreateReportAsync("error", log);
    }

    public async Task<StorageFile?> CreateCrashReportAsync()
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

        return await CreateReportAsync("crash", reportBody);
    }

    private async Task<StorageFile> CreateReportAsync(string reportType, string body)
    {
        var builder = new StringBuilder();
        builder.AppendLine("App Version: " + appVersion.Major + "." + appVersion.Minor + "." + appVersion.Build);
        builder.AppendLine("OS Version: " + Environment.OSVersion.VersionString);
        builder.AppendLine();
        builder.AppendLine(body);
        string report = builder.ToString();
        var filePath = Path.Combine(Path.GetTempPath(), $"universe-photos-{reportType}-report.txt");
        File.WriteAllText(filePath, report);
        return await StorageFile.GetFileFromPathAsync(filePath);
    }

    private async Task SendMailAsync(string subject, string body)
    {
        using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential("universe.photos.app@gmail.com", CompileTimeConstants.GMailAppPassword)
        };

        var emailMessage = new MailMessage
        {
            From = new MailAddress("universe.photos.app@gmail.com"),
            To = { "universe-photos@outlook.de" },
            Subject = subject,
            Body = body
        };

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
