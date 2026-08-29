using Essentials.NET.Logging;
using NSubstitute;
using PhotoViewer.Core.Services;
using Xunit;

namespace PhotoViewer.Test.Services;

public class ErrorReportServiceTest
{
    private readonly ErrorReportService errorReportService;

    private readonly IEventLogService eventLogService = Substitute.For<IEventLogService>();

    public ErrorReportServiceTest()
    {
        errorReportService = new ErrorReportService(nameof(ErrorReportServiceTest), default, eventLogService);
    }

    [Fact(Skip = "Skip test because it sends an email")]
    public async Task SendErrorReport()
    {
        eventLogService.GetErrorsSinceLastCheck().Returns(["error"]);
        string logFolderPath = TestUtils.CreateTestFolder(nameof(ErrorReportServiceTest), nameof(CreateCrashReport_NoLogFile));
        using var _ = TestUtils.RegisterLogger(new Logger([new FileAppender(logFolderPath)]));
        var reportFile = await errorReportService.CreateCrashReportAsync();
        await errorReportService.SendErrorReportAsync(reportFile!);
    }

    [Fact]
    public async Task CreateCrashReport_NoLogFile()
    {
        eventLogService.GetErrorsSinceLastCheck().Returns(["error"]);
        string logFolderPath = TestUtils.CreateTestFolder(nameof(ErrorReportServiceTest), nameof(CreateCrashReport_NoLogFile));
        using var _ = TestUtils.RegisterLogger(new Logger([new FileAppender(logFolderPath)]));

        var reportFile = await errorReportService.CreateCrashReportAsync();

        Assert.NotNull(reportFile);
        string expectedReport = "error\n\nno log file found";
        Assert.Contains(expectedReport, File.ReadAllText(reportFile.Path));
    }

    [Fact]
    public async Task CreateCrashReport_WithLogFile()
    {
        eventLogService.GetErrorsSinceLastCheck().Returns(["error"]);
        string logFolderPath = TestUtils.CreateTestFolder(nameof(ErrorReportServiceTest), nameof(CreateCrashReport_WithLogFile));
        using var _ = TestUtils.RegisterLogger(new Logger([new FileAppender(logFolderPath)]));
        Log.Info("make sure the current log exists");

        CreateLogFile(logFolderPath, DateTimeOffset.Now.AddMinutes(-7), "log of crash");

        // newer files
        CreateLogFile(logFolderPath, DateTimeOffset.Now.AddMinutes(5), "newer log");
        CreateLogFile(logFolderPath, DateTimeOffset.Now.AddMinutes(10), "newer log");

        // other files in use
        CreateLogFileInUse(logFolderPath, DateTimeOffset.Now.AddMinutes(-5), "log in use");
        CreateLogFileInUse(logFolderPath, DateTimeOffset.Now.AddMinutes(-10), "log in use");

        // archived files
        CreateArchivedLogFile(logFolderPath, DateTimeOffset.Now.AddMinutes(-5), "archived log");
        CreateArchivedLogFile(logFolderPath, DateTimeOffset.Now.AddMinutes(-10), "archived log");

        var reportFile = await errorReportService.CreateCrashReportAsync();

        Assert.NotNull(reportFile);
        string expectedReport = "error\n\nlog of crash";
        Assert.Contains(expectedReport, File.ReadAllText(reportFile.Path));
    }

    private void CreateLogFile(string logFolderPath, DateTimeOffset date, string content)
    {
        File.WriteAllText(Path.Combine(logFolderPath, $"log-{date:yyyy-MM-dd-HH-mm-ss}.txt"), content);
    }

    private void CreateLogFileInUse(string logFolderPath, DateTimeOffset date, string content)
    {
        string logFilePath = Path.Combine(logFolderPath, $"log-{date:yyyy-MM-dd-HH-mm-ss}.txt");
        File.WriteAllText(logFilePath, content);
        new FileStream(logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
    }

    private void CreateArchivedLogFile(string logFolderPath, DateTimeOffset date, string content)
    {
        File.WriteAllText(Path.Combine(logFolderPath, $"log-{date:yyyy-MM-dd-HH-mm-ss}.txt.bak"), content);
    }

}
