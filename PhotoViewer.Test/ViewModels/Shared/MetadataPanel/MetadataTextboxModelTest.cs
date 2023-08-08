using CommunityToolkit.Mvvm.Messaging;
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class MetadataTextboxModelTest
{
    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly TimerMock timerMock = new TimerMock();

    private readonly IMetadataService metadataServiceMock = Substitute.For<IMetadataService>();

    private readonly IMetadataProperty<string> metadataPropertyMock = Substitute.For<IMetadataProperty<string>>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly IBackgroundTaskService backgroundTaskService = Substitute.For<IBackgroundTaskService>();

    private readonly SynchronizationContextMock synchronizationContextMock = new SynchronizationContextMock();

    private MetadataTextboxModel metadataTextboxModel;

    public MetadataTextboxModelTest()
    {
        using var _ = synchronizationContextMock.Apply();
        Log.Logger = Substitute.For<ILogger>();
        metadataPropertyMock.Identifier.Returns("test-property");
        TimerFactory timerFactory = (interval, autoRestart) =>
        {
            Assert.False(autoRestart);
            return timerMock;
        };
        metadataTextboxModel = new MetadataTextboxModel(messenger, metadataServiceMock, dialogService, backgroundTaskService, metadataPropertyMock, timerFactory);
        TestUtils.CheckSynchronizationContextOfPropertyChangedEvents(metadataTextboxModel, synchronizationContextMock);
    }

    [Fact]
    public async Task SetText_WritesValueAndSetsIsWriting()
    {
        using var _ = synchronizationContextMock.Apply();

        var fileMock = Substitute.For<IBitmapFileInfo>();
        metadataServiceMock
            .GetMetadataAsync(fileMock, metadataPropertyMock)
            .Returns("value from file");
        var tsc = new TaskCompletionSource();
        metadataServiceMock
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value")
            .Returns(tsc.Task); 

        var metadata = new Dictionary<string, object?>() { { "test-property", "value from file" } };
        metadataTextboxModel.UpdateFilesChanged(ImmutableList.Create(fileMock), new[] { new MetadataView(metadata) });

        metadataTextboxModel.Text = "some value";

        Assert.Equal(1, timerMock.RestartCount);
        Assert.False(metadataTextboxModel.IsWriting);

        await timerMock.RaiseElapsed();
        await metadataTextboxModel.LastDispatchTask;

        Assert.True(metadataTextboxModel.IsWriting);
        
        tsc.SetResult();
        await metadataTextboxModel.LastWriteFilesTask;
        
        Assert.False(metadataTextboxModel.IsWriting);
        await metadataServiceMock.ReceivedOnce().WriteMetadataAsync(fileMock, metadataPropertyMock, "some value");
    }

    [Fact]
    public async Task SetText_WritesOnlyLastValue()
    {
        using var _ = synchronizationContextMock.Apply();

        var fileMock = Substitute.For<IBitmapFileInfo>();
        metadataServiceMock
            .GetMetadataAsync(fileMock, metadataPropertyMock)
            .Returns("value from file");
        var metadata = new Dictionary<string, object?>() { { "test-property", "value from file" } };
        metadataTextboxModel.UpdateFilesChanged(ImmutableList.Create(fileMock), new[] { new MetadataView(metadata) });

        metadataTextboxModel.Text = "some value 01";
        Assert.Equal(1, timerMock.RestartCount);

        metadataTextboxModel.Text = "some value 02";
        Assert.Equal(2, timerMock.RestartCount);

        metadataTextboxModel.Text = "some value 03";
        Assert.Equal(3, timerMock.RestartCount);

        await timerMock.RaiseElapsed();
        await metadataTextboxModel.LastWriteFilesTask;

        await metadataServiceMock.DidNotReceive()
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value 01");
        await metadataServiceMock.DidNotReceive()
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value 01");
        await metadataServiceMock.ReceivedOnce()
            .WriteMetadataAsync(fileMock, metadataPropertyMock, "some value 03");
    }

    [Fact]
    public async Task UpdateFilesChanged_WritesUnsavedValue()
    {
        using var _ = synchronizationContextMock.Apply();

        var file = Substitute.For<IBitmapFileInfo>();
        metadataServiceMock.GetMetadataAsync(file, metadataPropertyMock).Returns("value from file");
        metadataTextboxModel.UpdateFilesChanged(ImmutableList.Create(file), new[] { CreateMetadataView("value from file") });

        metadataTextboxModel.Text = "some value";
        Assert.Equal(1, timerMock.RestartCount);
        var otherFile = Substitute.For<IBitmapFileInfo>();
        metadataTextboxModel.UpdateFilesChanged(ImmutableList.Create(otherFile), new[] { CreateMetadataView("value from other file") });
        await metadataTextboxModel.LastWriteFilesTask;

        Assert.False(timerMock.IsEnabled);
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
