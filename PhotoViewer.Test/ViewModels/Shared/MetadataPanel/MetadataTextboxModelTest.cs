using MetadataAPI;
using Newtonsoft.Json.Linq;
using NSubstitute;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class MetadataTextboxModelTest
{
    private readonly ITimer timerMock = Substitute.For<ITimer>();

    private readonly IMetadataService metadataServiceMock = Substitute.For<IMetadataService>();

    private readonly IMetadataProperty<string> metadataPropertyMock = Substitute.For<IMetadataProperty<string>>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly IBackgroundTaskService backgroundTaskService = Substitute.For<IBackgroundTaskService>();

    private readonly MetadataTextboxModel metadataTextboxModel;

    public MetadataTextboxModelTest()
    {
        Log.Logger = Substitute.For<ILogger>();
        metadataPropertyMock.Identifier.Returns("test-property");
        TimerFactory timerFactory = (interval, autoRestart) =>
        {
            Assert.False(autoRestart);
            return timerMock;
        };
        metadataTextboxModel = new MetadataTextboxModel(metadataServiceMock, dialogService, backgroundTaskService, metadataPropertyMock, timerFactory);
    }

    [Fact]
    public async void SetText()
    {
        var fileMock = Substitute.For<IBitmapFileInfo>();
        metadataServiceMock
            .GetMetadataAsync(fileMock, metadataPropertyMock)
            .Returns("value from file");
        var tsc = new TaskCompletionSource();
        metadataServiceMock
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value")
            .Returns(tsc.Task)
            .AndDoes(_ => Assert.True(metadataTextboxModel.IsWriting));         
       
        var metadata = new Dictionary<string, object?>() { { "test-property", "value from file" } };
        metadataTextboxModel.UpdateFilesChanged(new[] { fileMock }, new[] { new MetadataView(metadata) });
        timerMock.IsEnabled.Returns(false);

        metadataTextboxModel.Text = "some value";

        timerMock.Received().Restart();
        timerMock.IsEnabled.Returns(true);
        Assert.False(metadataTextboxModel.IsWriting);

        timerMock.Elapsed += Raise.EventWith(EventArgs.Empty);

        tsc.SetResult();
        await metadataTextboxModel.WriteFilesTask;
        await Task.Delay(1);
        await metadataTextboxModel.LastDispatchTask;

        Assert.False(metadataTextboxModel.IsWriting);

        // TODO: verify no errors
    }

    [Fact]
    public async Task SetText_MultipleTimes()
    {
        var fileMock = Substitute.For<IBitmapFileInfo>();
        metadataServiceMock
            .GetMetadataAsync(fileMock, metadataPropertyMock)
            .Returns("value from file");
        string? lastWrittenValue = null;
        metadataServiceMock
            .WriteMetadataAsync(fileMock, metadataPropertyMock, Arg.Any<string>())
            .Returns(Task.CompletedTask)
            .AndDoes(args => { lastWrittenValue = (string)args[2]; });
        var metadata = new Dictionary<string, object?>() { { "test-property", "value from file" } };
        metadataTextboxModel.UpdateFilesChanged(new[] { fileMock }, new[] { new MetadataView(metadata) });

        metadataTextboxModel.Text = "some value 01";
        timerMock.Received().Restart();
        timerMock.IsEnabled.Returns(true);

        metadataTextboxModel.Text = "some value 02";
        timerMock.Received().Restart();

        metadataTextboxModel.Text = "some value 03";
        timerMock.Received().Restart();

        timerMock.Elapsed += Raise.EventWith(EventArgs.Empty);

        await metadataTextboxModel.WriteFilesTask;
        await metadataServiceMock.DidNotReceive()
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value 01");
        await metadataServiceMock.DidNotReceive()
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value 01");
        await metadataServiceMock.Received(1)
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value 03");
    }

    [Fact]
    public async Task UnsavedValueIsWrittenWhenFilesChanging()
    {
        var file = Substitute.For<IBitmapFileInfo>();
        metadataServiceMock.GetMetadataAsync(file, metadataPropertyMock).Returns("value from file");
        metadataTextboxModel.UpdateFilesChanged(new[] { file }, new[] { CreateMetadataView("value from file") });

        metadataTextboxModel.Text = "some value";
        timerMock.Received().Restart();
        timerMock.IsEnabled.Returns(true);
        var otherFile = Substitute.For<IBitmapFileInfo>();
        metadataTextboxModel.UpdateFilesChanged(new[] { otherFile }, new[] { CreateMetadataView("value from other file") });
      
        timerMock.Received().Stop();
        await Task.Delay(10); // TODO
        await metadataServiceMock.Received().WriteMetadataAsync(file, metadataPropertyMock, "some value");
        Assert.Equal("value from other file", metadataTextboxModel.Text);
    }

    // TODO test error handling

    private MetadataView CreateMetadataView(string value)
    {
        return new MetadataView(new Dictionary<string, object?>
        {
            { metadataPropertyMock.Identifier, value }
        });
    }

}
