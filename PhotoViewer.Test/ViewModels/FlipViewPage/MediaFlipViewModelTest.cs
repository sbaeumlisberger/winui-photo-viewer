using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using Xunit;

namespace PhotoViewer.Test.ViewModels.FlipViewPage;

public class MediaFlipViewModelTest
{
    private readonly Func<IMediaFileInfo, IMediaFlipViewItemModel> itemModelFactory;

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly MediaFlipViewModel mediaFlipViewModel;

    public MediaFlipViewModelTest()
    {
        Log.Logger = Substitute.For<ILogger>();

        itemModelFactory = (mediaFile) =>
        {
            var mock = Substitute.For<IMediaFlipViewItemModel>();
            mock.MediaFile.Returns(mediaFile);
            return mock;
        };

        mediaFlipViewModel = new MediaFlipViewModel(messenger, null!, null!, itemModelFactory, new ApplicationSettings());
    }

    [Fact]
    public async void Receive_MediaFilesLoadedMessage()
    {
        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[17];

        var tsc = new TaskCompletionSource<LoadMediaFilesResult>();

        messenger.Send(new MediaFilesLoadingMessage(new LoadMediaFilesTask(startFile, tsc.Task)));
        await mediaFlipViewModel.LastDispatchTask;

        Assert.NotNull(mediaFlipViewModel.SelectedItemModel);
        Assert.Equal(startFile, mediaFlipViewModel.SelectedItemModel.MediaFile);
        Assert.Single(mediaFlipViewModel.Items);
        Assert.Single(mediaFlipViewModel.ItemModels);
        Assert.True(mediaFlipViewModel.IsLoadingMoreFiles);

        tsc.SetResult(new LoadMediaFilesResult(files, startFile));

        Assert.NotNull(mediaFlipViewModel.SelectedItemModel);
        Assert.Equal(startFile, mediaFlipViewModel.SelectedItemModel.MediaFile);
        Assert.Equal(files.Count, mediaFlipViewModel.Items.Count);
        Assert.Equal(5, mediaFlipViewModel.ItemModels.Count);
        Assert.NotNull(mediaFlipViewModel.TryGetItemModel(startFile));
        Assert.False(mediaFlipViewModel.IsLoadingMoreFiles);
    }

    [Fact]
    public async void PreloadedItemModelIsResused_WhenLoadingMediaFilesCompleted()
    {
        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[17];

        var tsc = new TaskCompletionSource<LoadMediaFilesResult>();

        messenger.Send(new MediaFilesLoadingMessage(new LoadMediaFilesTask(startFile, tsc.Task)));
        await mediaFlipViewModel.LastDispatchTask;

        Assert.NotNull(mediaFlipViewModel.SelectedItemModel);
        var preloadItemModel = mediaFlipViewModel.SelectedItemModel;

        tsc.SetResult(new LoadMediaFilesResult(files, startFile));

        Assert.Equal(preloadItemModel, mediaFlipViewModel.SelectedItemModel);
        preloadItemModel.DidNotReceive().Cleanup();
    }

    [Fact]
    public async void Receive_MediaFilesLoadedMessage_DeleteStartFileWhileLoadingMoreFiles()
    {
        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[17];

        var tsc = new TaskCompletionSource<LoadMediaFilesResult>();

        messenger.Send(new MediaFilesLoadingMessage(new LoadMediaFilesTask(startFile, tsc.Task)));
        await mediaFlipViewModel.LastDispatchTask;

        Assert.NotNull(mediaFlipViewModel.SelectedItemModel);
        Assert.Equal(startFile, mediaFlipViewModel.SelectedItemModel.MediaFile);
        Assert.Single(mediaFlipViewModel.Items);
        Assert.Single(mediaFlipViewModel.ItemModels);
        Assert.True(mediaFlipViewModel.IsLoadingMoreFiles);

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));
        await mediaFlipViewModel.LastDispatchTask;

        Assert.Null(mediaFlipViewModel.SelectedItem);
        Assert.Null(mediaFlipViewModel.SelectedItemModel);

        tsc.SetResult(new LoadMediaFilesResult(files, startFile));

        Assert.NotNull(mediaFlipViewModel.SelectedItemModel);
        Assert.Equal(startFile, mediaFlipViewModel.SelectedItemModel.MediaFile);
        Assert.Equal(files.Count, mediaFlipViewModel.Items.Count);
        Assert.Equal(5, mediaFlipViewModel.ItemModels.Count);
        Assert.NotNull(mediaFlipViewModel.TryGetItemModel(startFile));
        Assert.False(mediaFlipViewModel.IsLoadingMoreFiles);
    }

    [Fact]
    public async void Receive_MediaFilesDeletedMessage()
    {
        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[17];
        mediaFlipViewModel.SetFiles(files, startFile);
        bool selectionValid = true;
        mediaFlipViewModel.PropertyChanged += (_, _) =>
        {
            if (!mediaFlipViewModel.Items.Contains(mediaFlipViewModel.SelectedItem!))
            {
                selectionValid = false;
            }
        };

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));
        await mediaFlipViewModel.LastDispatchTask;

        var expectedSelectedFile = files[18];
        Assert.Equal(files.Count - 1, mediaFlipViewModel.Items.Count);
        Assert.Equal(expectedSelectedFile, mediaFlipViewModel.SelectedItem);
        Assert.NotNull(mediaFlipViewModel.SelectedItemModel);
        Assert.Equal(5, mediaFlipViewModel.ItemModels.Count);
        Assert.NotNull(mediaFlipViewModel.TryGetItemModel(expectedSelectedFile));
        Assert.True(selectionValid);
    }

    [Fact]
    public async void Receive_MediaFilesDeletedMessage_LastItem()
    {
        var files = Enumerable.Range(0, 200).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[199];

        mediaFlipViewModel.SetFiles(files, startFile);

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));
        await mediaFlipViewModel.LastDispatchTask;

        var expectedSelectedFile = files[198];
        Assert.Equal(files.Count - 1, mediaFlipViewModel.Items.Count);
        Assert.Equal(expectedSelectedFile, mediaFlipViewModel.SelectedItem);
        Assert.NotNull(mediaFlipViewModel.SelectedItemModel);
        Assert.Equal(3, mediaFlipViewModel.ItemModels.Count);
        Assert.NotNull(mediaFlipViewModel.TryGetItemModel(expectedSelectedFile));
    }

    [Fact]
    public async void Receive_MediaFilesDeletedMessage_Empty()
    {
        var files = Enumerable.Range(0, 1).Select(i => MockMediaFileInfo("File_" + i + ".jpg")).ToList();
        IMediaFileInfo startFile = files[0];

        mediaFlipViewModel.SetFiles(files, startFile);

        messenger.Send(new MediaFilesDeletedMessage(new[] { startFile }));
        await mediaFlipViewModel.LastDispatchTask;

        Assert.Empty(mediaFlipViewModel.Items);
        Assert.Null(mediaFlipViewModel.SelectedItem);
        Assert.Null(mediaFlipViewModel.SelectedItemModel);
        Assert.Empty(mediaFlipViewModel.ItemModels);
    }

    private IMediaFileInfo MockMediaFileInfo(string fileName)
    {
        var mock = Substitute.For<IMediaFileInfo>();
        mock.DisplayName.Returns(fileName);
        mock.FilePath.Returns("Test/" + fileName);
        return mock;
    }
}