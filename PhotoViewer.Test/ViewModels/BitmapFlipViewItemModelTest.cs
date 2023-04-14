using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Imaging;
using Moq;
using NSubstitute;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace PhotoViewer.Test.ViewModels;

public class BitmapFlipViewItemModelTest
{
    private readonly Mock<IBitmapFileInfo> bitmapFileMock = new();

    private readonly Mock<IMediaFileContextMenuModel> contextMenuMock = new();

    private readonly Mock<ITagPeopleToolModel> tagPeopleToolModelMock = new();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly Mock<IImageLoaderService> imageLoadServiceMock = new();

    private readonly BitmapFlipViewItemModel bitmapFlipViewItemModel;

    public BitmapFlipViewItemModelTest()
    {
        Log.Logger = Mock.Of<ILogger>();

        bitmapFileMock.Setup(m => m.IsMetadataSupported).Returns(true);

        var viewModelFactory = Substitute.For<IViewModelFactory>();
        viewModelFactory.CreateTagPeopleToolModel(bitmapFileMock.Object).Returns(tagPeopleToolModelMock.Object);

        bitmapFlipViewItemModel = new BitmapFlipViewItemModel(bitmapFileMock.Object, contextMenuMock.Object, viewModelFactory, messenger, imageLoadServiceMock.Object);
    }

    [Fact]
    public async Task InitializeAsync()
    {
        var loadFromFileAsyncTSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.Setup(m => m.LoadFromFileAsync(bitmapFileMock.Object, It.IsAny<CancellationToken>())).Returns(loadFromFileAsyncTSC.Task);
        var bitmapImageLoadedMessageCapture = TestUtils.CaptureMessage<BitmapImageLoadedMessage>(messenger);

        var initializeTask = bitmapFlipViewItemModel.InitializeAsync();

        Assert.True(bitmapFlipViewItemModel.IsLoading);
        Assert.False(bitmapFlipViewItemModel.IsLoadingImageFailed);
        Assert.Empty(bitmapFlipViewItemModel.ErrorMessage);

        var bitmapImage = Mock.Of<IBitmapImageModel>();
        loadFromFileAsyncTSC.SetResult(bitmapImage);
        await initializeTask;

        Assert.Equal(bitmapImage, bitmapFlipViewItemModel.BitmapImage);
        Assert.False(bitmapFlipViewItemModel.IsLoading);
        Assert.False(bitmapFlipViewItemModel.IsLoadingImageFailed);
        Assert.Empty(bitmapFlipViewItemModel.ErrorMessage);
        Assert.Equal(bitmapFileMock.Object, bitmapImageLoadedMessageCapture.Message?.BitmapFile);
        Assert.Equal(bitmapImage, bitmapImageLoadedMessageCapture.Message?.BitmapImage);

        tagPeopleToolModelMock.Verify(m => m.InitializeAsync());
    }


    [Fact]
    public async Task InitializeAsync_Error()
    {
        var loadFromFileAsyncTSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.Setup(m => m.LoadFromFileAsync(bitmapFileMock.Object, It.IsAny<CancellationToken>())).Returns(loadFromFileAsyncTSC.Task);
        var bitmapImageLoadedMessageCapture = TestUtils.CaptureMessage<BitmapImageLoadedMessage>(messenger);

        var initializeTask = bitmapFlipViewItemModel.InitializeAsync();

        Assert.True(bitmapFlipViewItemModel.IsLoading);
        Assert.False(bitmapFlipViewItemModel.IsLoadingImageFailed);
        Assert.Empty(bitmapFlipViewItemModel.ErrorMessage);

        string errorMessage = "some error message";
        loadFromFileAsyncTSC.SetException(new Exception(errorMessage));
        await initializeTask;

        Assert.Null(bitmapFlipViewItemModel.BitmapImage);
        Assert.False(bitmapFlipViewItemModel.IsLoading);
        Assert.True(bitmapFlipViewItemModel.IsLoadingImageFailed);
        Assert.Equal(errorMessage, bitmapFlipViewItemModel.ErrorMessage);
        Assert.Null(bitmapImageLoadedMessageCapture.Message);

        tagPeopleToolModelMock.Verify(m => m.InitializeAsync());
    }

    [Fact]
    public async Task Receive_BitmapRotatedMesssage()
    {
        imageLoadServiceMock
            .Setup(m => m.LoadFromFileAsync(bitmapFileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IBitmapImageModel>());
        await bitmapFlipViewItemModel.InitializeAsync();

        var loadFromFileAsyncTSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock
            .Setup(m => m.LoadFromFileAsync(bitmapFileMock.Object, It.IsAny<CancellationToken>()))
            .Returns(loadFromFileAsyncTSC.Task);

        messenger.Send(new BitmapRotatedMesssage(bitmapFileMock.Object));

        Assert.True(bitmapFlipViewItemModel.IsLoading);

        var bitmapImage = Mock.Of<IBitmapImageModel>();
        loadFromFileAsyncTSC.SetResult(bitmapImage);

        Assert.False(bitmapFlipViewItemModel.IsLoading);
        Assert.Equal(bitmapImage, bitmapFlipViewItemModel.BitmapImage);
    }

    [Fact]
    public async Task Receive_BitmapRotatedMesssage_Twice()
    {
        imageLoadServiceMock
            .Setup(m => m.LoadFromFileAsync(bitmapFileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IBitmapImageModel>());
        await bitmapFlipViewItemModel.InitializeAsync();

        var loadFromFileAsyncTSC1 = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock
            .Setup(m => m.LoadFromFileAsync(bitmapFileMock.Object, It.IsAny<CancellationToken>()))
            .Returns(loadFromFileAsyncTSC1.Task);

        messenger.Send(new BitmapRotatedMesssage(bitmapFileMock.Object));

        var loadFromFileAsyncTSC2 = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock
            .Setup(m => m.LoadFromFileAsync(bitmapFileMock.Object, It.IsAny<CancellationToken>()))
            .Returns(loadFromFileAsyncTSC2.Task);

        messenger.Send(new BitmapRotatedMesssage(bitmapFileMock.Object));

        Assert.True(bitmapFlipViewItemModel.IsLoading);

        var bitmapImage1 = Mock.Of<IBitmapImageModel>();
        loadFromFileAsyncTSC1.SetResult(bitmapImage1);

        var bitmapImage2 = Mock.Of<IBitmapImageModel>();
        loadFromFileAsyncTSC2.SetResult(bitmapImage2);

        Assert.False(bitmapFlipViewItemModel.IsLoading);
        Assert.Equal(bitmapImage2, bitmapFlipViewItemModel.BitmapImage);
    }

    [Fact]
    public void Receive_BitmapRotatedMesssage_OtherFile()
    {
        messenger.Send(new BitmapRotatedMesssage(Mock.Of<IBitmapFileInfo>()));

        Assert.False(bitmapFlipViewItemModel.IsLoading);
    }

    [Fact]
    public void Receive_SetIsSelected()
    {
        bitmapFlipViewItemModel.IsSelected = true;

        tagPeopleToolModelMock.VerifySet(m => m.IsEnabled = true);

        bitmapFlipViewItemModel.IsSelected = false;

        tagPeopleToolModelMock.VerifySet(m => m.IsEnabled = false);
    }
}
