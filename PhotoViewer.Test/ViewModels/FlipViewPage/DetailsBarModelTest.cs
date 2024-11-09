using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using MetadataAPI;
using MetadataAPI.Data;
using NSubstitute;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System.Globalization;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Xunit;

namespace PhotoViewer.Test.ViewModels.FlipViewPage;

public class DetailsBarModelTest
{

    private readonly DetailsBarModel detailsBarModel;

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IMetadataService metadataService = Substitute.For<IMetadataService>();

    private readonly ApplicationSettings settings = new ApplicationSettings();

    private readonly IBitmapFileInfo bitmapFile;

    private readonly IBitmapFlipViewItemModel bitmapItemModel;

    private readonly IMediaFlipViewItemModel vectorGraphicItemModel;

    public DetailsBarModelTest()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

        bitmapFile = Substitute.For<IBitmapFileInfo>();
        bitmapFile.DisplayName.Returns("Test File.jpg");
        bitmapFile.GetFileSizeAsync().Returns(3_534_172UL);
        bitmapFile.GetSizeInPixelsAsync().Returns(new Size(4912, 3264));
        bitmapFile.IsMetadataSupported.Returns(true);
        var metadata = new MetadataView(new Dictionary<string, object?>()
        {
            { MetadataProperties.DateTaken.Identifier, new DateTime(2020, 10, 07, 15, 44, 23) },
            { MetadataProperties.ExposureTime.Identifier, new Fraction(1, 200) },
            { MetadataProperties.FNumber.Identifier, new Fraction(52, 10) },
            { MetadataProperties.ISOSpeed.Identifier, (ushort)400 },
            { MetadataProperties.FocalLength.Identifier, (double)18 },
            { MetadataProperties.FocalLengthInFilm.Identifier, (ushort)27 }
        });
        metadataService.GetMetadataAsync(bitmapFile).Returns(metadata);
        bitmapItemModel = Substitute.For<IBitmapFlipViewItemModel>();
        bitmapItemModel.MediaFile.Returns(bitmapFile);
        bitmapItemModel.ImageViewModel.Image.Returns((IBitmapImageModel?)null);

        var vectorGraphicFile = Substitute.For<IVectorGraphicFileInfo>();
        vectorGraphicFile.DisplayName.Returns("Test File.svg");
        vectorGraphicFile.GetFileSizeAsync().Returns(3_534_172UL);
        vectorGraphicFile.GetSizeInPixelsAsync().Returns(Size.Empty);
        vectorGraphicFile.GetDateModifiedAsync().Returns(new DateTime(2020, 08, 13, 11, 22, 39));
        vectorGraphicItemModel = Substitute.For<IMediaFlipViewItemModel>();
        vectorGraphicItemModel.MediaFile.Returns(vectorGraphicFile);

        detailsBarModel = new DetailsBarModel(messenger, metadataService, settings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialVisibilityIsRetrievedFromSettings(bool autoOpenDetailsBar)
    {
        settings.AutoOpenDetailsBar = autoOpenDetailsBar;

        var detailsBarModel = new DetailsBarModel(messenger, metadataService, settings);

        Assert.Equal(autoOpenDetailsBar, detailsBarModel.IsVisible);
    }

    [Fact]
    public void InitialyShowsNoInformationAvailableMessage()
    {
        Assert.True(detailsBarModel.ShowNoInformationAvailableMessage);
    }

    [Fact]
    public void UpdatesDetailsFromBitmapFile_WhenSelectedItemModelChanged()
    {
        detailsBarModel.IsVisible = true;

        detailsBarModel.SelectedItemModel = bitmapItemModel;

        // TODO assert first cleared

        Assert.False(detailsBarModel.ShowNoInformationAvailableMessage);
        Assert.Equal("Test File.jpg", detailsBarModel.FileName);
        Assert.Equal("07.10.2020 15:44", detailsBarModel.DateFormatted);
        Assert.Equal("3,53 MB", detailsBarModel.FileSize);
        Assert.Equal("1/200s F5,2 ISO400 18(27)mm", detailsBarModel.CameraDetails);
        Assert.Equal("4912x3264px", detailsBarModel.SizeInPixels);
        Assert.Equal(ColorSpaceType.NotSpecified, detailsBarModel.ColorSpaceType);
        Assert.False(detailsBarModel.ShowColorProfileIndicator);
    }

    [Fact]
    public void UpdatesDetailsFromVectorGraphicFile_WhenSelectedItemModelChanged()
    {
        detailsBarModel.IsVisible = true;

        detailsBarModel.SelectedItemModel = vectorGraphicItemModel;

        Assert.False(detailsBarModel.ShowNoInformationAvailableMessage);
        Assert.Equal("Test File.svg", detailsBarModel.FileName);
        Assert.Equal("13.08.2020 11:22", detailsBarModel.DateFormatted);
        Assert.Equal("3,53 MB", detailsBarModel.FileSize);
        Assert.Empty(detailsBarModel.CameraDetails);
        Assert.Empty(detailsBarModel.SizeInPixels);
        Assert.Equal(ColorSpaceType.NotSpecified, detailsBarModel.ColorSpaceType);
        Assert.False(detailsBarModel.ShowColorProfileIndicator);
    }

    // TODO video file

    [Fact]
    public void UpdatesDetailsFromImage_WhenSelectedItemModelChanged()
    {
        var bitmapImage = Substitute.For<IBitmapImageModel>();
        bitmapImage.ColorSpace.Returns(new ColorSpaceInfo(ColorSpaceType.AdobeRGB, new byte[0]));
        bitmapImage.SizeInPixels.Returns(new BitmapSize(4912, 3264));
        bitmapItemModel.ImageViewModel.Image.Returns(bitmapImage);
        detailsBarModel.IsVisible = true;

        detailsBarModel.SelectedItemModel = bitmapItemModel;

        Assert.Equal(ColorSpaceType.AdobeRGB, detailsBarModel.ColorSpaceType);
        Assert.True(detailsBarModel.ShowColorProfileIndicator);
    }

    [Fact]
    public void DoesNotUpdate_WhenSelectedItemModelChangedButNotVisible()
    {
        detailsBarModel.IsVisible = false;

        detailsBarModel.SelectedItemModel = bitmapItemModel;

        AssertDetailsEmpty();
    }

    [Fact]
    public void UpdatesDetails_WhenIsVisibleChangedToTrue()
    {
        detailsBarModel.SelectedItemModel = bitmapItemModel;

        detailsBarModel.IsVisible = true;

        Assert.Equal("Test File.jpg", detailsBarModel.FileName);
    }

    [Fact]
    public void ClearsDetails_WhenIsVisibleChangedToFalse()
    {
        detailsBarModel.IsVisible = true;
        detailsBarModel.SelectedItemModel = bitmapItemModel;

        detailsBarModel.IsVisible = false;

        AssertDetailsEmpty();
    }

    [Fact]
    public async Task UpdatesDateFormatted_WhenDateTakenModified()
    {
        detailsBarModel.IsVisible = true;
        detailsBarModel.SelectedItemModel = bitmapItemModel;
        var metadata = new MetadataView(new Dictionary<string, object?>()
        {
            { MetadataProperties.DateTaken.Identifier, new DateTime(2021, 06, 13, 20, 32, 17) }
        });
        metadataService.GetMetadataAsync(bitmapFile).Returns(metadata);

        messenger.Send(new MetadataModifiedMessage(new[] { bitmapFile }, MetadataProperties.DateTaken));

        await detailsBarModel.LastDispatchTask;
        Assert.Equal("13.06.2021 20:32", detailsBarModel.DateFormatted);
    }


    [Fact]
    public void DoesNotUpdateDateFormatted_WhenDateOfOtherFileChanged()
    {
        detailsBarModel.IsVisible = true;
        detailsBarModel.SelectedItemModel = bitmapItemModel;
        metadataService.ClearReceivedCalls();
        var otherFile = Substitute.For<IBitmapFileInfo>();
        var metadata = new MetadataView(new Dictionary<string, object?>()
        {
            { MetadataProperties.DateTaken.Identifier, new DateTime(2021, 06, 13, 20, 32, 17) }
        });
        metadataService.GetMetadataAsync(otherFile).Returns(metadata);

        messenger.Send(new MetadataModifiedMessage(new[] { otherFile }, MetadataProperties.DateTaken));

        Assert.Equal("07.10.2020 15:44", detailsBarModel.DateFormatted);
        metadataService.DidNotReceive().GetMetadataAsync(bitmapFile);
    }

    [Fact]
    public void DoesNotUpdateDateFormatted_WhenOtherMetadataPropertyChanged()
    {
        detailsBarModel.IsVisible = true;
        detailsBarModel.SelectedItemModel = bitmapItemModel;
        metadataService.ClearReceivedCalls();

        messenger.Send(new MetadataModifiedMessage(new[] { bitmapFile }, MetadataProperties.Keywords));

        Assert.Equal("07.10.2020 15:44", detailsBarModel.DateFormatted);
        metadataService.DidNotReceive().GetMetadataAsync(bitmapFile);
    }

    [Fact]
    public async Task UpdatesColorSpaceInfo_WhenBitmapImageLoaded()
    {
        detailsBarModel.IsVisible = true;
        detailsBarModel.SelectedItemModel = bitmapItemModel;
        var bitmapImage = Substitute.For<IBitmapImageModel>();
        bitmapImage.ColorSpace.Returns(new ColorSpaceInfo(ColorSpaceType.AdobeRGB, new byte[0]));

        messenger.Send(new BitmapImageLoadedMessage(bitmapFile, bitmapImage));
        await detailsBarModel.LastDispatchTask;

        Assert.Equal(ColorSpaceType.AdobeRGB, detailsBarModel.ColorSpaceType);
        Assert.True(detailsBarModel.ShowColorProfileIndicator);
    }


    [Fact]
    public void DoesNotUpdateColorSpaceInfo_WhenOtherBitmapImageLoaded()
    {
        detailsBarModel.IsVisible = true;
        detailsBarModel.SelectedItemModel = bitmapItemModel;
        var bitmapImage = Substitute.For<IBitmapImageModel>();
        bitmapImage.ColorSpace.Returns(new ColorSpaceInfo(ColorSpaceType.AdobeRGB, new byte[0]));

        messenger.Send(new BitmapImageLoadedMessage(Substitute.For<IBitmapFileInfo>(), bitmapImage));

        Assert.Equal(ColorSpaceType.NotSpecified, detailsBarModel.ColorSpaceType);
        Assert.False(detailsBarModel.ShowColorProfileIndicator);
    }

    [Fact]
    public async Task UpdatesFileName_WhenMediaFilesLoaded()
    {
        detailsBarModel.IsVisible = true;
        detailsBarModel.SelectedItemModel = bitmapItemModel;
        bitmapFile.DisplayName.Returns("Test File.jpg[.arw]");

        messenger.Send(new MediaFilesLoadingMessage(LoadMediaFilesTask.Empty));
        await detailsBarModel.LastDispatchTask;

        Assert.Equal("Test File.jpg[.arw]", detailsBarModel.FileName);
    }

    // TODO test cancel and errors

    private void AssertDetailsEmpty()
    {
        Assert.Empty(detailsBarModel.FileName);
        Assert.Empty(detailsBarModel.DateFormatted);
        Assert.Empty(detailsBarModel.CameraDetails);
        Assert.Empty(detailsBarModel.FileSize);
        Assert.Empty(detailsBarModel.SizeInPixels);
        Assert.Equal(ColorSpaceType.NotSpecified, detailsBarModel.ColorSpaceType);
        Assert.False(detailsBarModel.ShowColorProfileIndicator);
    }
}