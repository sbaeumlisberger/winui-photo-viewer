using PhotoViewer.Core.Services;
using Xunit;

namespace PhotoViewer.Test.Services;

public class ErrorReportServiceTest
{
    [Fact(Skip = "Skip test because it sends an email to gmail")]
    public async Task SendErrorReport()
    {
        var errorReportService = new ErrorReportService();
   
        await errorReportService.SendErrorReportAsync("Test");    
    }

}
