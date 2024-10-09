using NSubstitute;
using PhotoViewer.Core.Models;
using Windows.Foundation;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test.Models;

public class MediaFileInfoBaseTest
{

    private class MediaFileInfo : MediaFileInfoBase
    {
        public override IReadOnlyList<IStorageFile> LinkedStorageFiles { get; }

        public MediaFileInfo(IStorageFile file, IReadOnlyList<IStorageFile> linkedFiles) : base(file)
        {
            LinkedStorageFiles = linkedFiles;
        }

        public override Task<Size> GetSizeInPixelsAsync()
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task RenamesAllStorageFilesWhenRenamed()
    {
        var storageFile = MockStorageFile("TestFile.jpg");
        var linkedStorageFiles = new[]
        {
            MockStorageFile("TestFile.arw"),
            MockStorageFile("TestFile.xmp")
        };
        var mediaFile = new MediaFileInfo(storageFile, linkedStorageFiles);

        await mediaFile.RenameAsync("NewName");

        _ = storageFile.Received().RenameAsync("NewName.jpg");
        _ = linkedStorageFiles[0].Received().RenameAsync("NewName.arw");
        _ = linkedStorageFiles[1].Received().RenameAsync("NewName.xmp");
    }

    private IStorageFile MockStorageFile(string fileName)
    {
        var storageFile = Substitute.For<IStorageFile>();
        storageFile.Name.Returns(fileName);
        storageFile.FileType.Returns(Path.GetExtension(fileName));
        storageFile.RenameAsync(Arg.Any<string>()).Returns(Task.CompletedTask.AsAsyncAction());
        return storageFile;
    }

}
