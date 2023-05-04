﻿using CommunityToolkit.Mvvm.Messaging;
using Moq;
using NSubstitute;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using PhotoViewer.Core.Services;
using Xunit.Abstractions;

namespace PhotoViewer.Test.ViewModels;

public class ImageViewModelTest
{
    private readonly IBitmapFileInfo bitmapFileMock = Substitute.For<IBitmapFileInfo>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly ICachedImageLoaderService imageLoadServiceMock = Substitute.For<ICachedImageLoaderService>();

    private readonly ImageViewModel imageViewModel;

    public ImageViewModelTest(ITestOutputHelper testOutputHelper)
    {
        Log.Logger = new LoggerMock(testOutputHelper); 

        bitmapFileMock.IsMetadataSupported.Returns(true);

        imageViewModel = new ImageViewModel(bitmapFileMock, imageLoadServiceMock, messenger);
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

        var bitmapImage = Mock.Of<IBitmapImageModel>();
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
        Assert.Null(bitmapImageLoadedMessageCapture.Message);
    }

    [Fact]
    public async Task Receive_BitmapModifiedMesssage()
    {
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), false)
            .Returns(Mock.Of<IBitmapImageModel>());
        await imageViewModel.InitializeAsync();

        var loadFromFileAsyncTSC = new TaskCompletionSource<IBitmapImageModel>();
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), true)
            .Returns(loadFromFileAsyncTSC.Task);

        messenger.Send(new BitmapModifiedMesssage(bitmapFileMock));
        await imageViewModel.LastDispatchTask;

        Assert.True(imageViewModel.IsLoading);

        var bitmapImage = Mock.Of<IBitmapImageModel>();
        loadFromFileAsyncTSC.SetResult(bitmapImage);

        Assert.False(imageViewModel.IsLoading);
        Assert.Equal(bitmapImage, imageViewModel.Image);
    }

    [Fact]
    public async Task Receive_BitmapModifiedMesssage_Twice()
    {
        imageLoadServiceMock.LoadFromFileAsync(bitmapFileMock, Arg.Any<CancellationToken>(), false)
            .Returns(Mock.Of<IBitmapImageModel>());
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

        var bitmapImage1 = Mock.Of<IBitmapImageModel>();
        loadFromFileAsyncTSC1.SetResult(bitmapImage1);

        var bitmapImage2 = Mock.Of<IBitmapImageModel>();
        loadFromFileAsyncTSC2.SetResult(bitmapImage2);

        Assert.False(imageViewModel.IsLoading);
        Assert.Equal(bitmapImage2, imageViewModel.Image);
    }

    [Fact]
    public void Receive_BitmapRotatedMesssage_OtherFile()
    {
        messenger.Send(new BitmapModifiedMesssage(Mock.Of<IBitmapFileInfo>()));

        Assert.False(imageViewModel.IsLoading);
    }
}
