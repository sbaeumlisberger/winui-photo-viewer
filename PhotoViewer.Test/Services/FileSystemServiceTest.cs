using PhotoViewer.Core.Services;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test.Services;

public class FileSystemServiceTest
{
    private readonly FileSystemService fileSystemService = new FileSystemService();

    [Fact]
    public async Task Restore()
    {
        var testFolderPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testFolderPath, "test.txt");
        File.WriteAllText(testFilePath, "test");
        var creationTime = File.GetCreationTime(testFilePath);
        var storageFile = await StorageFile.GetFileFromPathAsync(testFilePath);
        await storageFile.DeleteAsync();
        Assert.False(File.Exists(testFilePath));

        fileSystemService.Restore(storageFile);

        Assert.True(File.Exists(testFilePath));
        Assert.Equal("test", File.ReadAllText(testFilePath));
        Assert.Equal(creationTime, File.GetCreationTime(testFilePath));
    }
}
