using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System.Collections.Immutable;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class MetadataTextboxModelTest
{
    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly FakeTimeProvider timeProvider = Substitute.ForPartsOf<FakeTimeProvider>();

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
        metadataTextboxModel = new MetadataTextboxModel(messenger, metadataServiceMock, dialogService, backgroundTaskService, metadataPropertyMock, timeProvider);
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

        Assert.False(metadataTextboxModel.IsWriting);
        timeProvider.ReceivedOnce().CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>());

        timeProvider.Advance(MetadataTextboxModel.WaitTime);

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
        timeProvider.Received(1).CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>());

        metadataTextboxModel.Text = "some value 02";
        timeProvider.Received(2).CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>());

        metadataTextboxModel.Text = "some value 03";
        timeProvider.Received(3).CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>());

        timeProvider.Advance(MetadataTextboxModel.WaitTime);
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
        timeProvider.Received(1).CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>());

        var otherFile = Substitute.For<IBitmapFileInfo>();
        metadataTextboxModel.UpdateFilesChanged(ImmutableList.Create(otherFile), new[] { CreateMetadataView("value from other file") });
        await metadataTextboxModel.LastWriteFilesTask;

        await metadataServiceMock.Received().WriteMetadataAsync(file, metadataPropertyMock, "some value");
        Assert.Equal("value from other file", metadataTextboxModel.Text);

        metadataServiceMock.ClearReceivedCalls();

        timeProvider.Advance(MetadataTextboxModel.WaitTime);
        await metadataServiceMock.DidNotReceive().WriteMetadataAsync(file, metadataPropertyMock, Arg.Any<string>());
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
