using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using System.Collections.Immutable;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test.ViewModels.Dialogs;

public class ImportGpxTrackDialogModelTest
{
    private readonly IMessenger messenger = new StrongReferenceMessenger();

    private readonly IDialogService dialogService = Substitute.For<IDialogService>();

    private readonly IGpxService gpxService = Substitute.For<IGpxService>();

    [Fact]
    public async Task AppliesImportedGpxTrackToFiles()
    {
        var files = ImmutableList.Create(MockBitmapFile(), MockBitmapFile(), MockBitmapFile());
        var gpxFile = Substitute.For<IStorageFile>();
        dialogService.When(x => x.ShowDialogAsync(Arg.Any<FileOpenPickerModel>()))
            .Do(callInfo => callInfo.Arg<FileOpenPickerModel>().File = gpxFile);
        var gpxTrack = new GpxTrack(new List<GpxTrackPoint>());
        gpxService.ReadTrackFromGpxFileAsync(gpxFile).Returns(gpxTrack);
        gpxService.TryApplyGpxTrackToFile(gpxTrack, files[0]).Returns(true);
        gpxService.TryApplyGpxTrackToFile(gpxTrack, files[1]).Returns(false);
        gpxService.TryApplyGpxTrackToFile(gpxTrack, files[2]).Returns(true);
        var metadataModifiedMessageCapture = TestUtils.CaptureMessage<MetadataModifiedMessage>(messenger);

        var importGpxTrackDialogModel = new ImportGpxTrackDialogModel(messenger, dialogService, gpxService, files);
        importGpxTrackDialogModel.BrowseFileCommand.Execute(null);
        await importGpxTrackDialogModel.ImportCommand.ExecuteAsync(null);

        Assert.True(importGpxTrackDialogModel.ShowSuccessMessage);
        await gpxService.Received().TryApplyGpxTrackToFile(gpxTrack, files[0]);
        await gpxService.Received().TryApplyGpxTrackToFile(gpxTrack, files[1]);
        await gpxService.Received().TryApplyGpxTrackToFile(gpxTrack, files[2]);
        Assert.NotNull(metadataModifiedMessageCapture.Message);
        Assert.Equal(2, metadataModifiedMessageCapture.Message.Files.Count);
        Assert.Equal(MetadataProperties.GeoTag, metadataModifiedMessageCapture.Message.MetadataProperty);
    }

    [Fact]
    public async Task ShowsErrorMessageForGpxFileParseErrors()
    {
        var files = ImmutableList.Create(MockBitmapFile(), MockBitmapFile(), MockBitmapFile());
        var gpxFile = Substitute.For<IStorageFile>();
        dialogService.When(x => x.ShowDialogAsync(Arg.Any<FileOpenPickerModel>()))
            .Do(callInfo => callInfo.Arg<FileOpenPickerModel>().File = gpxFile);
        string errorMessage = "Some Error Message";
        gpxService.ReadTrackFromGpxFileAsync(gpxFile).ThrowsAsync(new Exception(errorMessage));

        var importGpxTrackDialogModel = new ImportGpxTrackDialogModel(messenger, dialogService, gpxService, files);
        importGpxTrackDialogModel.BrowseFileCommand.Execute(null);
        await importGpxTrackDialogModel.ImportCommand.ExecuteAsync(null);

        Assert.True(importGpxTrackDialogModel.ShowErrorMessage);
        Assert.Single(importGpxTrackDialogModel.Errors);
        Assert.Contains(importGpxTrackDialogModel.Errors, error => error.Contains(errorMessage));
    }

    private IBitmapFileInfo MockBitmapFile()
    {
        var mock = Substitute.For<IBitmapFileInfo>();
        mock.IsMetadataSupported.Returns(true);
        return mock;
    }
}
