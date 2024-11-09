using Essentials.NET.Logging;
using Xunit.Abstractions;

namespace PhotoViewer.Test;

internal class TestOutputAppender : ILogAppender
{
    private readonly ITestOutputHelper testOutputHelper;

    public TestOutputAppender(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    public void Append(LogLevel level, string message)
    {
        testOutputHelper.WriteLine(message);
    }

    public void Dispose()
    {
    }
}
