using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public class CrashReportService
{
    public static async Task TrySendCrashReportAsync()
    {
        try
        {
            var version = Package.Current.Id.Version;
            var logFile = await Log.GetLogFileAsync().ConfigureAwait(false);

            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine("App Version: " + version.Major + "." + version.Minor + "." + version.Build);
            bodyBuilder.AppendLine("OS Version: " + Environment.OSVersion.VersionString);
            bodyBuilder.AppendLine();
            bodyBuilder.AppendLine(await FileIO.ReadTextAsync(logFile).AsTask().ConfigureAwait(false));

            using var smtpClient = new SmtpClient("smtp-mail.outlook.com", 587) { EnableSsl = true };
            smtpClient.Credentials = new NetworkCredential("universe-photos@outlook.com", "tEa8wTr7gz0k"); // TODO protect password

            var emailMessage = new MailMessage();
            emailMessage.From = new MailAddress("universe-photos@outlook.com");
            emailMessage.To.Add("s.baeumlisberger@live.de");
            emailMessage.Subject = $"WinUI Photo Viewer Crash Report {DateTime.Now:g}";
            emailMessage.Body = bodyBuilder.ToString();

            await smtpClient.SendMailAsync(emailMessage).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to send crash report: " + ex);
        }
    }


}
