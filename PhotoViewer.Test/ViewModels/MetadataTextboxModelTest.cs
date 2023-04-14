using MetadataAPI;
using Moq;
using Newtonsoft.Json.Linq;
using NSubstitute;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Tocronx.SimpleAsync;
using Xunit;

namespace PhotoViewer.Test.ViewModels;

public class MetadataTextboxModelTest
{

    private readonly SequentialTaskRunner writeFilesRunner = new();

    private readonly Mock<ITimer> timerMock = new();

    private readonly Mock<IMetadataService> metadataServiceMock = new();

    private readonly Mock<IMetadataProperty<string>> metadataPropertyMock = new();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly MetadataTextboxModel metadataTextboxModel;

    public MetadataTextboxModelTest()
    {
        Log.Logger = Mock.Of<ILogger>();
        metadataPropertyMock.SetupGet(m => m.Identifier).Returns("test-property");
        TimerFactory timerFactory = (interval, autoRestart) =>
        {
            Assert.False(autoRestart);
            return timerMock.Object;
        };
        metadataTextboxModel = new MetadataTextboxModel(writeFilesRunner, metadataServiceMock.Object, dialogService, metadataPropertyMock.Object, timerFactory);
    }

    [Fact]
    public async Task SetText()
    {
        var fileMock = new Mock<IBitmapFileInfo>();
        metadataServiceMock
            .Setup(m => m.GetMetadataAsync(fileMock.Object, metadataPropertyMock.Object))
            .ReturnsAsync("value from file");
        var tsc = new TaskCompletionSource();
        metadataServiceMock            
            .Setup(m => m.WriteMetadataAsync(fileMock.Object, metadataPropertyMock.Object, "some value"))
            .Callback(() => Assert.True(metadataTextboxModel.IsWriting))
            .Returns(tsc.Task);
        var metadata = new Dictionary<string, object?>() { { "test-property", "value from file" } };
        metadataTextboxModel.UpdateFilesChanged(new[] { fileMock.Object }, new[] { new MetadataView(metadata) });
        timerMock.SetupGet(m => m.IsEnabled).Returns(false);

        metadataTextboxModel.Text = "some value";

        timerMock.Verify(m => m.Restart());
        timerMock.SetupGet(m => m.IsEnabled).Returns(true);
        Assert.False(metadataTextboxModel.IsWriting);

        timerMock.Raise(m => m.Elapsed += null, EventArgs.Empty);

        tsc.SetResult();

        await metadataTextboxModel.WriteTask;

        Assert.False(metadataTextboxModel.IsWriting);

        // TODO: verify no errors
    }

    [Fact]
    public async Task SetText_MultipleTimes()
    {
        var fileMock = new Mock<IBitmapFileInfo>();
        metadataServiceMock
            .Setup(m => m.GetMetadataAsync(fileMock.Object, metadataPropertyMock.Object))
            .ReturnsAsync("value from file");
        string? lastWrittenValue = null;
        metadataServiceMock
            .Setup(m => m.WriteMetadataAsync(fileMock.Object, metadataPropertyMock.Object, It.IsAny<string>()))
            .Callback((IInvocation invocation) => { lastWrittenValue = (string)invocation.Arguments[2]; });
        var metadata = new Dictionary<string, object?>() { { "test-property", "value from file" } };
        metadataTextboxModel.UpdateFilesChanged(new[] { fileMock.Object }, new[] { new MetadataView(metadata) });

        metadataTextboxModel.Text = "some value 01";
        timerMock.Verify(m => m.Restart());
        timerMock.SetupGet(m => m.IsEnabled).Returns(true);

        metadataTextboxModel.Text = "some value 02";
        timerMock.Verify(m => m.Restart());

        metadataTextboxModel.Text = "some value 03";
        timerMock.Verify(m => m.Restart());

        timerMock.Raise(m => m.Elapsed += null, EventArgs.Empty);        
        await metadataTextboxModel.WriteTask;

        metadataServiceMock.Verify(
            m => m.WriteMetadataAsync(fileMock.Object, metadataPropertyMock.Object, "some value 01"),
            Times.Never);
        metadataServiceMock.Verify(
            m => m.WriteMetadataAsync(fileMock.Object, metadataPropertyMock.Object, "some value 01"),
            Times.Never);
        metadataServiceMock.Verify(
            m => m.WriteMetadataAsync(fileMock.Object, metadataPropertyMock.Object, "some value 03"),
            Times.Once);
    }

    [Fact]
    public void UnsavedValueIsWrittenWhenFilesChanging()
    {
        var file = Mock.Of<IBitmapFileInfo>();
        metadataTextboxModel.UpdateFilesChanged(new[] { file }, new[] { CreateMetadataView("value from file") });

        metadataTextboxModel.Text = "some value";
        timerMock.Verify(m => m.Restart());
        timerMock.SetupGet(m => m.IsEnabled).Returns(true);
        var otherFile = Mock.Of<IBitmapFileInfo>();
        metadataTextboxModel.UpdateFilesChanged(new[] { otherFile }, new[] { CreateMetadataView("value from other file") });

        metadataServiceMock.Verify(m => m.WriteMetadataAsync(file, metadataPropertyMock.Object, "some value"));
        Assert.Equal("value from other file", metadataTextboxModel.Text);
    }

    // TODO test error handling

    private MetadataView CreateMetadataView(string value)
    {
        return new MetadataView(new Dictionary<string, object?>
        {
            { metadataPropertyMock.Object.Identifier, value }
        });
    }

}
