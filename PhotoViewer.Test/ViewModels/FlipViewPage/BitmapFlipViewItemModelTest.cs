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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace PhotoViewer.Test.ViewModels.FlipViewPage;

public class BitmapFlipViewItemModelTest
{
    private readonly IBitmapFileInfo bitmapFileMock = Substitute.For<IBitmapFileInfo>();

    private readonly IMediaFileContextMenuModel contextMenuMock = Substitute.For<IMediaFileContextMenuModel>();

    private readonly ITagPeopleToolModel tagPeopleToolModelMock = Substitute.For<ITagPeopleToolModel>();

    private readonly IImageViewModel imageViewModel = Substitute.For<IImageViewModel>();

    private readonly ICropImageToolModel cropImageToolModel = Substitute.For<ICropImageToolModel>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly BitmapFlipViewItemModel bitmapFlipViewItemModel;

    public BitmapFlipViewItemModelTest()
    {
        Log.Logger = Mock.Of<ILogger>();

        bitmapFileMock.IsMetadataSupported.Returns(true);

        var viewModelFactory = Substitute.For<IViewModelFactory>();
        viewModelFactory.CreateTagPeopleToolModel(bitmapFileMock).Returns(tagPeopleToolModelMock);
        viewModelFactory.CreateMediaFileContextMenuModel().Returns(contextMenuMock);
        viewModelFactory.CreateImageViewModel(bitmapFileMock).Returns(imageViewModel);
        viewModelFactory.CreateCropImageToolModel(bitmapFileMock).Returns(cropImageToolModel);

        bitmapFlipViewItemModel = new BitmapFlipViewItemModel(bitmapFileMock, viewModelFactory, messenger);
    }

    [Fact]
    public void ImageIsPassedToTagPeopleTool()
    {
        var image = Substitute.For<IBitmapImageModel>();
        imageViewModel.Image.Returns(image);
        imageViewModel.RaisePropertyChanged(nameof(IImageViewModel.Image));

        Assert.Equal(image, tagPeopleToolModelMock.BitmapImage);
    }

    [Fact]
    public async Task InitializeAsync()
    {
        await bitmapFlipViewItemModel.InitializeAsync();

        await tagPeopleToolModelMock.Received().InitializeAsync();
        await imageViewModel.Received().InitializeAsync();
    }

    [Fact]
    public void IsSelectedIsPassedToChildViewModels()
    {
        bitmapFlipViewItemModel.IsSelected = true;

        Assert.True(tagPeopleToolModelMock.IsEnabled);
        Assert.True(cropImageToolModel.IsEnabled);

        bitmapFlipViewItemModel.IsSelected = false;

        Assert.False(tagPeopleToolModelMock.IsEnabled);
        Assert.False(cropImageToolModel.IsEnabled);
    }
}
