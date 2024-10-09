using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
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
        cropImageToolModel = new CropImageToolModel(bitmapFile, messenger, cropImageService, dialogService);
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

    // TODO add more tests
}
