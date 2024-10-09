using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using NSubstitute;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System.Collections.Immutable;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Shared.MetadataPanel;

public class RatingSectionModelTest
{
    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IBackgroundTaskService backgroundTaskService = Substitute.For<IBackgroundTaskService>();

    private readonly RatingSectionModel ratingSectionModel;

    public RatingSectionModelTest()
    {
        ratingSectionModel = new RatingSectionModel(metadataService, dialogService, messenger, backgroundTaskService);
    }

    [Fact]
    public void SetsRating_WhenFilesChanged_OneFile()
    {
        var raisedPropertyChangedEvents = TestUtils.CapturePropertyChangedEvents(ratingSectionModel);
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var metadata = new[] { CreateMetadataView(3) };

        ratingSectionModel.UpdateFilesChanged(files, metadata);

        Assert.Equal(3, ratingSectionModel.Rating);
        Assert.Contains(nameof(ratingSectionModel.Rating), raisedPropertyChangedEvents);
    }

    [Fact]
    public void SetsRatingToZero_WhenFilesChanged_DifferentRatings()
    {
        ratingSectionModel.UpdateFilesChanged(Substitute.For<IImmutableList<IBitmapFileInfo>>(), new[] { CreateMetadataView(3) });
        var raisedPropertyChangedEvents = TestUtils.CapturePropertyChangedEvents(ratingSectionModel);
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var metadata = new[]
        {
            CreateMetadataView(3),
            CreateMetadataView(1),
            CreateMetadataView(5),
        };

        ratingSectionModel.UpdateFilesChanged(files, metadata);

        Assert.Equal(0, ratingSectionModel.Rating);
        Assert.Contains(nameof(ratingSectionModel.Rating), raisedPropertyChangedEvents);
    }

    [Fact]
    public void SetsRating_WhenFilesChanged_EqualRatings()
    {
        var raisedPropertyChangedEvents = TestUtils.CapturePropertyChangedEvents(ratingSectionModel);
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var metadata = new[]
        {
            CreateMetadataView(5),
            CreateMetadataView(5),
            CreateMetadataView(5),
        };

        ratingSectionModel.UpdateFilesChanged(files, metadata);

        Assert.Equal(5, ratingSectionModel.Rating);
        Assert.Contains(nameof(ratingSectionModel.Rating), raisedPropertyChangedEvents);
    }

    [Fact]
    public void SetsRating_WhenMetadataModified()
    {
        var raisedPropertyChangedEvents = TestUtils.CapturePropertyChangedEvents(ratingSectionModel);
        var files = Substitute.For<IImmutableList<IBitmapFileInfo>>();
        var metadata = new[] { CreateMetadataView(3) };
        ratingSectionModel.UpdateFilesChanged(files, metadata);

        UpdateMetadataView(metadata[0], 5);
        ratingSectionModel.UpdateMetadataModified(MetadataProperties.Rating);

        Assert.Equal(5, ratingSectionModel.Rating);
        Assert.Contains(nameof(ratingSectionModel.Rating), raisedPropertyChangedEvents);
    }

    [Fact]
    public async Task WritesRatingToFiles_WhenRatingChangedFromExternal()
    {
        var files = ImmutableList.Create
        (
            MockBitmapFileInfo(0),
            MockBitmapFileInfo(3),
            MockBitmapFileInfo(5)
        );
        var metadata = new[]
        {
            CreateMetadataView(0),
            CreateMetadataView(3),
            CreateMetadataView(5),
        };
        ratingSectionModel.UpdateFilesChanged(files, metadata);

        ratingSectionModel.Rating = 3;

        await ratingSectionModel.LastWriteFilesTask;
        await VerifyReceivedWriteMetadataAsync(files[0], 3);
        await VerifyNotReceivedWriteMetadataAsync(files[1]);
        await VerifyReceivedWriteMetadataAsync(files[2], 3);
    }

    [Fact]
    public void DoesNotWritesRatingToFiles_WhenFilesUpdated()
    {
        var files = ImmutableList.Create(MockBitmapFileInfo(3));
        var metadata = new[] { CreateMetadataView(3) };
        ratingSectionModel.UpdateFilesChanged(files, metadata);

        metadataService.DidNotReceive().WriteMetadataAsync(Arg.Any<IBitmapFileInfo>(), MetadataProperties.Rating, Arg.Any<int>());
    }

    private MetadataView CreateMetadataView(int rating)
    {
        return new MetadataView(new Dictionary<string, object?>()
        {
            { MetadataProperties.Rating.Identifier, rating }
        });
    }

    private void UpdateMetadataView(MetadataView metadataView, int rating)
    {
        metadataView.Source[MetadataProperties.Rating.Identifier] = rating;
    }

    private IBitmapFileInfo MockBitmapFileInfo(int rating)
    {
        var file = Substitute.For<IBitmapFileInfo>();
        metadataService.GetMetadataAsync(file, MetadataProperties.Rating).Returns(rating);
        return file;
    }

    private async Task VerifyNotReceivedWriteMetadataAsync(IBitmapFileInfo file)
    {
        await metadataService.DidNotReceive().WriteMetadataAsync(file, MetadataProperties.Rating, Arg.Any<int>());
    }

    private async Task VerifyReceivedWriteMetadataAsync(IBitmapFileInfo file, int rating)
    {
        await metadataService.Received().WriteMetadataAsync(
            file,
            MetadataProperties.Rating,
            Arg.Is(rating));
    }
}
