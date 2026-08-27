using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Streams;
using Xunit;

namespace PhotoViewer.Test.ViewModels.FlipViewPage;

public class CropImageToolModelTest
{

    private readonly CropImageToolModel cropImageToolModel;

    readonly IBitmapFileInfo bitmapFile = Substitute.For<IBitmapFileInfo>();

    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly ICropImageService cropImageService = Substitute.For<ICropImageService>();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    public CropImageToolModelTest()
    {
        bitmapFile.GetSizeInPixelsAsync().Returns(Task.FromResult(new Size(3000, 2000)));
        cropImageToolModel = new CropImageToolModel(bitmapFile, messenger, cropImageService, dialogService);
        cropImageToolModel.IsEnabled = true;
    }

    [Fact]
    public void InitialSelectionIsCorrect()
    {
        Assert.Equal(new RectInt32(300, 200, 2400, 1600), cropImageToolModel.SelectionInPixels);
    }

    [Fact]
    public void IsSetActiveWhenToggleCropImageToolMessageReceived()
    {
        messenger.Send(new ToggleCropImageToolMessage());

        Assert.True(cropImageToolModel.IsActive);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-1)]
    [InlineData(0)]
    public void SettingInvalidSelectionWidthIsPrevented(double invalidValue)
    {
        var propertyChangedEvents = TestUtils.CapturePropertyChangedEvents(cropImageToolModel);
        messenger.Send(new ToggleCropImageToolMessage());

        cropImageToolModel.SelectionWidthInPixels = invalidValue;

        Assert.Equal(2400, cropImageToolModel.SelectionWidthInPixels);
        Assert.Contains(nameof(cropImageToolModel.SelectionWidthInPixels), propertyChangedEvents);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-1)]
    [InlineData(0)]
    public void SettingInvalidSelectionHeightIsPrevented(double invalidValue)
    {
        var propertyChangedEvents = TestUtils.CapturePropertyChangedEvents(cropImageToolModel);
        messenger.Send(new ToggleCropImageToolMessage());

        cropImageToolModel.SelectionHeightInPixels = invalidValue;

        Assert.Equal(1600, cropImageToolModel.SelectionHeightInPixels);
        Assert.Contains(nameof(cropImageToolModel.SelectionHeightInPixels), propertyChangedEvents);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-1)]
    [InlineData(0)]
    public void SettingInvalidAspectRatioWidthIsPrevented(double invalidValue)
    {
        var propertyChangedEvents = TestUtils.CapturePropertyChangedEvents(cropImageToolModel);
        messenger.Send(new ToggleCropImageToolMessage());

        cropImageToolModel.FixedAspectRatioWidth = invalidValue;

        Assert.Equal(3, cropImageToolModel.FixedAspectRatioWidth);
        Assert.Contains(nameof(cropImageToolModel.FixedAspectRatioWidth), propertyChangedEvents);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-1)]
    [InlineData(0)]
    public void SettingInvalidAspectRatioHeightIsPrevented(double invalidValue)
    {
        var propertyChangedEvents = TestUtils.CapturePropertyChangedEvents(cropImageToolModel);
        messenger.Send(new ToggleCropImageToolMessage());

        cropImageToolModel.FixedAspectRatioHeight = invalidValue;

        Assert.Equal(2, cropImageToolModel.FixedAspectRatioHeight);
        Assert.Contains(nameof(cropImageToolModel.FixedAspectRatioHeight), propertyChangedEvents);
    }

    [Fact]
    public void SelectionIsUpdatedWhenAspectRatioWidthChanged()
    {
        var propertyChangedEvents = TestUtils.CapturePropertyChangedEvents(cropImageToolModel);
        cropImageToolModel.AspectRatioMode = AspectRatioMode.Fixed;

        cropImageToolModel.FixedAspectRatioWidth = 2;

        Assert.Equal(new RectInt32(300, 200, 1600, 1600), cropImageToolModel.SelectionInPixels);
        Assert.Contains(nameof(cropImageToolModel.SelectionInPixels), propertyChangedEvents);
        Assert.Contains(nameof(cropImageToolModel.SelectionWidthInPixels), propertyChangedEvents);
    }

    [Fact]
    public void SelectionIsUpdatedWhenAspectRatioHeightChanged()
    {
        var propertyChangedEvents = TestUtils.CapturePropertyChangedEvents(cropImageToolModel);
        cropImageToolModel.AspectRatioMode = AspectRatioMode.Fixed;

        cropImageToolModel.FixedAspectRatioHeight = 1;

        Assert.Equal(new RectInt32(300, 200, 2400, 800), cropImageToolModel.SelectionInPixels);
        Assert.Contains(nameof(cropImageToolModel.SelectionInPixels), propertyChangedEvents);
        Assert.Contains(nameof(cropImageToolModel.FixedAspectRatioHeight), propertyChangedEvents);
    }

    [Fact]
    public void SettingSameSelectionAgainDoesNotCauseRoundingIssues()
    {
        cropImageToolModel.AspectRatioMode = AspectRatioMode.Fixed;
        cropImageToolModel.FixedAspectRatioWidth = 4912;
        cropImageToolModel.FixedAspectRatioHeight = 3264;

        cropImageToolModel.SelectionWidthInPixels = 1000;
        cropImageToolModel.SetSelectionInPixels(cropImageToolModel.SelectionInPixels);

        Assert.Equal(1000, cropImageToolModel.SelectionWidthInPixels);
        Assert.Equal(664, cropImageToolModel.SelectionHeightInPixels);
    }

    [Fact]
    public async Task SaveCopyCommandWritesToPickedFile()
    {
        var pickedfile = Substitute.For<IStorageFile>();
        dialogService.ShowDialogAsync(Arg.Any<FileSavePickerModel>())
            .Returns(Task.CompletedTask)
            .AndDoes(args => ((FileSavePickerModel)args[0]).File = pickedfile);

        using var fileStream = new InMemoryRandomAccessStream();
        pickedfile.OpenAsync(FileAccessMode.ReadWrite).ReturnsAsyncOperation<IRandomAccessStream>(fileStream);

        await cropImageToolModel.SaveCopyCommand.ExecuteAsync(null);

        await cropImageService.Received().CropImageAsync(bitmapFile, cropImageToolModel.SelectionInPixels, fileStream.AsStream());
    }
}
