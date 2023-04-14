using CommunityToolkit.Mvvm.Messaging;
using Moq;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using System.Diagnostics;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test.ViewModels;

public class MediaFlipViewModelTest
{
    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> itemModelFactory;

    public MediaFlipViewModelTest()
    {
        Log.Logger = Mock.Of<ILogger>();

        itemModelFactory = (mediaFile) =>
        {
            var mock = new Mock<IMediaFlipViewItemModel>();
            mock.SetupGet(m => m.MediaItem).Returns(mediaFile);
            return mock.Object;
        };
    }

    [Fact]
    public void Receive_MediaFilesLoadedMessage()
    {
        var messenger = new StrongReferenceMessenger();

        var flipViewPageModel = new MediaFlipViewModel(messenger, null!, null!, itemModelFactory, new ApplicationSettings());

        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[17];

        var tsc = new TaskCompletionSource<LoadMediaFilesResult>();

        messenger.Send(new MediaFilesLoadingMessage(new LoadMediaFilesTask(startFile, tsc.Task)));

        Assert.NotNull(flipViewPageModel.SelectedItemModel);
        Assert.Equal(startFile, flipViewPageModel.SelectedItemModel.MediaItem);
        Assert.Single(flipViewPageModel.Items);
        Assert.Single(flipViewPageModel.ItemModels);
        Assert.True(flipViewPageModel.IsLoadingMoreFiles);

        tsc.SetResult(new LoadMediaFilesResult(files, startFile));

        Assert.NotNull(flipViewPageModel.SelectedItemModel);
        Assert.Equal(startFile, flipViewPageModel.SelectedItemModel.MediaItem);
        Assert.Equal(files.Count, flipViewPageModel.Items.Count);
        Assert.Equal(5, flipViewPageModel.ItemModels.Count);
        Assert.NotNull(flipViewPageModel.TryGetItemModel(startFile));
        Assert.False(flipViewPageModel.IsLoadingMoreFiles);
    }

    [Fact]
    public void Receive_MediaFilesLoadedMessage_DeleteStartFileWhileLoadingMoreFiles()
    {
        var messenger = new StrongReferenceMessenger();

        var flipViewPageModel = new MediaFlipViewModel(messenger, null!, null!, itemModelFactory, new ApplicationSettings());

        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[17];

        var tsc = new TaskCompletionSource<LoadMediaFilesResult>();

        messenger.Send(new MediaFilesLoadingMessage(new LoadMediaFilesTask(startFile, tsc.Task)));

        Assert.NotNull(flipViewPageModel.SelectedItemModel);
        Assert.Equal(startFile, flipViewPageModel.SelectedItemModel.MediaItem);
        Assert.Single(flipViewPageModel.Items);
        Assert.Single(flipViewPageModel.ItemModels);
        Assert.True(flipViewPageModel.IsLoadingMoreFiles);

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));

        Assert.Null(flipViewPageModel.SelectedItem);
        Assert.Null(flipViewPageModel.SelectedItemModel);

        tsc.SetResult(new LoadMediaFilesResult(files, startFile));

        Assert.NotNull(flipViewPageModel.SelectedItemModel);
        Assert.Equal(startFile, flipViewPageModel.SelectedItemModel.MediaItem);
        Assert.Equal(files.Count, flipViewPageModel.Items.Count);
        Assert.Equal(5, flipViewPageModel.ItemModels.Count);
        Assert.NotNull(flipViewPageModel.TryGetItemModel(startFile));
        Assert.False(flipViewPageModel.IsLoadingMoreFiles);
    }

    [Fact]
    public void Receive_MediaFilesDeletedMessage()
    {
        var messenger = new StrongReferenceMessenger();

        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[17];

        var flipViewPageModel = new MediaFlipViewModel(messenger, null!, null!, itemModelFactory, new ApplicationSettings());
        flipViewPageModel.SetItems(files, startFile);

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));

        var expectedSelectedFile = files[18];
        Assert.Equal(files.Count - 1, flipViewPageModel.Items.Count);
        Assert.Equal(expectedSelectedFile, flipViewPageModel.SelectedItem);
        Assert.NotNull(flipViewPageModel.SelectedItemModel);
        Assert.Equal(5, flipViewPageModel.ItemModels.Count);
        Assert.NotNull(flipViewPageModel.TryGetItemModel(expectedSelectedFile));
    }

    [Fact]
    public void Receive_MediaFilesDeletedMessage_LastItem()
    {
        var messenger = new StrongReferenceMessenger();

        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[199];

        var flipViewPageModel = new MediaFlipViewModel(messenger, null!, null!, itemModelFactory, new ApplicationSettings());
        flipViewPageModel.SetItems(files, startFile);

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));

        var expectedSelectedFile = files[198];
        Assert.Equal(files.Count - 1, flipViewPageModel.Items.Count);
        Assert.Equal(expectedSelectedFile, flipViewPageModel.SelectedItem);
        Assert.NotNull(flipViewPageModel.SelectedItemModel);
        Assert.Equal(3, flipViewPageModel.ItemModels.Count);
        Assert.NotNull(flipViewPageModel.TryGetItemModel(expectedSelectedFile));
    }

    [Fact]
    public void Receive_MediaFilesDeletedMessage_Empty()
    {
        var messenger = new StrongReferenceMessenger();

        var files = Enumerable.Range(0, 1).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[0];

        var flipViewPageModel = new MediaFlipViewModel(messenger, null!, null!, itemModelFactory, new ApplicationSettings());
        flipViewPageModel.SetItems(files, startFile);

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));

        Assert.Empty(flipViewPageModel.Items);
        Assert.Null(flipViewPageModel.SelectedItem);
        Assert.Null(flipViewPageModel.SelectedItemModel);
        Assert.Empty(flipViewPageModel.ItemModels);
    }

    private IMediaFileInfo MockMediaFileInfo(string fileName)
    {
        var mock = new Mock<IMediaFileInfo>();
        mock.SetupGet(m => m.DisplayName).Returns(fileName);
        mock.SetupGet(m => m.FilePath).Returns("Test/" + fileName);
        return mock.Object;
    }
}