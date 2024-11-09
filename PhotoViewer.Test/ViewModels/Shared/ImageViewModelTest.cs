using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using NSubstitute;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace PhotoViewer.Test.ViewModels.Shared;

public class ImageViewModelTest : IDisposable
{
    private readonly IBitmapFileInfo bitmapFileMock = Substitute.For<IBitmapFileInfo>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly ICachedImageLoaderService imageLoadServiceMock = Substitute.For<ICachedImageLoaderService>();

    private readonly ImageViewModel imageViewModel;

    private readonly IDisposable loggerRegistration;

    public ImageViewModelTest(ITestOutputHelper testOutputHelper)
    {
        loggerRegistration = TestUtils.RegisterLogger(new Logger([new TestOutputAppender(testOutputHelper)]));

        bitmapFileMock.IsMetadataSupported.Returns(true);

        imageViewModel = new ImageViewModel(bitmapFileMock, imageLoadServiceMock, messenger);
    }

    public void Dispose()
    {
        loggerRegistration.Dispose();
    }

    [Fact]
    public async Task InitializeAsync()
    {
        var loadFromFileAsyncTSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), false).Returns(loadFromFileAsyncTSC.Task);
        var bitmapImageLoadedMessageCapture = TestUtils.CaptureMessage<BitmapImageLoadedMessage>(messenger);

        var initializeTask = imageViewModel.InitializeAsync();

        Assert.True(imageViewModel.IsLoading);
        Assert.False(imageViewModel.IsLoadingImageFailed);
        Assert.Empty(imageViewModel.ErrorMessage);

        var bitmapImage = Substitute.For<IBitmapImageModel>();
        loadFromFileAsyncTSC.SetResult(bitmapImage);
        await initializeTask;

        Assert.Equal(bitmapImage, imageViewModel.Image);
        Assert.False(imageViewModel.IsLoading);
        Assert.False(imageViewModel.IsLoadingImageFailed);
        Assert.Empty(imageViewModel.ErrorMessage);
        Assert.Equal(bitmapFileMock, bitmapImageLoadedMessageCapture.Message?.BitmapFile);
        Assert.Equal(bitmapImage, bitmapImageLoadedMessageCapture.Message?.BitmapImage);
    }

    [Fact]
    public async Task InitializeAsync_Error()
    {
        var loadFromFileAsyncTSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), false).Returns(loadFromFileAsyncTSC.Task);
        var bitmapImageLoadedMessageCapture = TestUtils.CaptureMessage<BitmapImageLoadedMessage>(messenger);

        var initializeTask = imageViewModel.InitializeAsync();

        Assert.True(imageViewModel.IsLoading);
        Assert.False(imageViewModel.IsLoadingImageFailed);
        Assert.Empty(imageViewModel.ErrorMessage);

        string errorMessage = "some error message";
        loadFromFileAsyncTSC.SetException(new Exception(errorMessage));
        await initializeTask;

        Assert.Null(imageViewModel.Image);
        Assert.False(imageViewModel.IsLoading);
        Assert.True(imageViewModel.IsLoadingImageFailed);
        Assert.Equal(errorMessage, imageViewModel.ErrorMessage);
        Assert.False(bitmapImageLoadedMessageCapture.IsMessageCaptured);
    }

    [Fact]
    public async Task Receive_BitmapModifiedMesssage()
    {
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), false)
            .Returns(Substitute.For<IBitmapImageModel>());
        await imageViewModel.InitializeAsync();

        var loadFromFileAsyncTSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), true)
            .Returns(loadFromFileAsyncTSC.Task);

        messenger.Send(new BitmapModifiedMesssage(bitmapFileMock));
        await imageViewModel.LastDispatchTask;

        Assert.True(imageViewModel.IsLoading);

        var bitmapImage = Substitute.For<IBitmapImageModel>();
        loadFromFileAsyncTSC.SetResult(bitmapImage);

        Assert.False(imageViewModel.IsLoading);
        Assert.Equal(bitmapImage, imageViewModel.Image);
    }

    [Fact]
    public async Task Receive_BitmapModifiedMesssage_Twice()
    {
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), false)
            .Returns(Substitute.For<IBitmapImageModel>());
        await imageViewModel.InitializeAsync();

        var loadFromFileAsyncTSC1 = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), true)
            .Returns(loadFromFileAsyncTSC1.Task);

        messenger.Send(new BitmapModifiedMesssage(bitmapFileMock));
        await imageViewModel.LastDispatchTask;
        await imageLoadServiceMock.Received().LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), true);

        var loadFromFileAsyncTSC2 = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), true)
            .Returns(loadFromFileAsyncTSC2.Task);

        messenger.Send(new BitmapModifiedMesssage(bitmapFileMock));
        await imageViewModel.LastDispatchTask;
        await imageLoadServiceMock.Received().LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), true);

        Assert.True(imageViewModel.IsLoading);

        var bitmapImage1 = Substitute.For<IBitmapImageModel>();
        loadFromFileAsyncTSC1.SetResult(bitmapImage1);

        var bitmapImage2 = Substitute.For<IBitmapImageModel>();
        loadFromFileAsyncTSC2.SetResult(bitmapImage2);

        await Task.Yield();

        Assert.False(imageViewModel.IsLoading);
        Assert.Equal(bitmapImage2, imageViewModel.Image);
    }

    [Fact]
    public void Receive_BitmapRotatedMesssage_OtherFile()
    {
        messenger.Send(new BitmapModifiedMesssage(Substitute.For<IBitmapFileInfo>()));

        Assert.False(imageViewModel.IsLoading);
    }
}
