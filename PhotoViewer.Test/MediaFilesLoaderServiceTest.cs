using Moq;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Services;
using Windows.Storage;
using Windows.Storage.Search;
using Xunit;
using FileAttributes = Windows.Storage.FileAttributes;

namespace PhotoViewer.Test;

public class MediaFilesLoaderServiceTest
{
    private const string FolderPath = @"A:\TestFolder";

    public readonly Mock<IFileSystemService> fileSystemServiceMock;

    private readonly MediaFilesLoaderService mediaFilesLoaderService;

    public MediaFilesLoaderServiceTest()
    {
        Log.Logger = Mock.Of<ILogger>();
        fileSystemServiceMock = new Mock<IFileSystemService>();
        mediaFilesLoaderService = new MediaFilesLoaderService(fileSystemServiceMock.Object);
    }

    [Fact]
    public async Task LoadMediaFilesFromFolder()
    {
        var files = new List<IStorageFile>
        {
            MockFile("File 01.jpg"),
            MockFile("File 02.jpg"),
            MockFile("File 02.arw"),
            MockFile("File 03.jpg"),
            MockFile("File 04.mp4")
        };
        var folder = MockFolder(files);
        var config = new LoadMediaConfig(false, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadMediaFilesFromFolder(folder, config);

        Assert.Null(loadMediaFilesTask.StartMediaFile);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        Assert.Null(result.StartMediaFile);

        var expectedMediaFiles = new[]
        {
            new []{ "File 01.jpg" },
            new []{ "File 02.jpg" },
            new []{ "File 02.arw" },
            new []{ "File 03.jpg" },
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }


    [Fact]
    public async Task LoadMediaFilesFromFilesQueryResult()
    {
        var files = new List<IStorageFile>
        {
            MockFile("File 01.jpg"),
            MockFile("File 02.jpg"),
            MockFile("File 03.jpg"),
            MockFile("File 04.jpg"),
            MockFile("File 05.jpg")
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[2]; // "File 03.jpg"
        var config = new LoadMediaConfig(false, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadMediaFilesFromFilesQueryResult(startFile, neighboringFilesQuery, config);

        Assert.NotNull(loadMediaFilesTask.StartMediaFile);
        Assert.Equal("File 03.jpg", loadMediaFilesTask.StartMediaFile.FileName);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        Assert.NotNull(result.StartMediaFile);
        Assert.Equal("File 03.jpg", result.StartMediaFile.FileName);

        var expectedMediaFiles = new[]
        {
            new []{ "File 01.jpg" },
            new []{ "File 02.jpg" },
            new []{ "File 03.jpg" },
            new []{ "File 04.jpg" },
            new []{ "File 05.jpg" },
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }

    [Fact]
    public async Task LoadMediaFilesFromFilesQueryResult_LinkRAWs()
    {
        var files = new List<IStorageFile>
        {
            MockFile("File 01.jpg"),
            MockFile("File 02.jpg"),
            MockFile("File 03.png"),
            MockFile("File 03.jpg"),
            MockFile("File 03.arw"),
            MockFile("File 04.jpg"),
            MockFile("File 04.arw"),
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[3]; // "File 03.jpg"
        var config = new LoadMediaConfig(true, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadMediaFilesFromFilesQueryResult(startFile, neighboringFilesQuery, config);

        Assert.NotNull(loadMediaFilesTask.StartMediaFile);
        Assert.Equal("File 03.jpg", loadMediaFilesTask.StartMediaFile.FileName);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        Assert.NotNull(result.StartMediaFile);
        AssertMediaFile(new[] { "File 03.jpg", "File 03.arw" }, result.StartMediaFile);

        var expectedMediaFiles = new[]
        {
            new []{ "File 01.jpg" },
            new []{ "File 02.jpg" },
            new []{ "File 03.png" },
            new []{ "File 03.jpg", "File 03.arw" },
            new []{ "File 04.jpg", "File 04.arw" }
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }

    [Fact]
    public async Task LoadMediaFilesFromFilesQueryResult_RAWsFolder()
    {
        var files = new List<IStorageFile>
        {
            MockFile("File 01.jpg"),
            MockFile("File 01.arw"),
            MockFile("File 02.jpg"),
            MockFile("File 03.jpg"),
            MockFile("File 04.jpg"),
        };
        var rawFilesInSubfolder = new List<IStorageFile>
        {
            MockFile("File 03.arw"),
            MockFile("File 04.arw"),
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[2]; // "File 02.jpg"
        var config = new LoadMediaConfig(true, "RAWs", false);
        MockRAWsFolder(rawFilesInSubfolder, config.RAWsFolderName!);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadMediaFilesFromFilesQueryResult(startFile, neighboringFilesQuery, config);

        Assert.NotNull(loadMediaFilesTask.StartMediaFile);
        Assert.Equal("File 02.jpg", loadMediaFilesTask.StartMediaFile.FileName);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        Assert.NotNull(result.StartMediaFile);
        AssertMediaFile(new[] { "File 02.jpg" }, result.StartMediaFile);

        var expectedMediaFiles = new[]
        {
            new []{ "File 01.jpg", "File 01.arw" },
            new []{ "File 02.jpg" },
            new []{ "File 03.jpg", "File 03.arw" },
            new []{ "File 04.jpg", "File 04.arw" }
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }

    [Fact]
    public async Task LoadMediaFilesFromFilesQueryResult_MTP()
    {
        var files = new List<IStorageFile>
        {
            MockFile("File 01.jpg"),
            MockFile("File 02.jpg"),
            MockFile("File 03.jpg"),
            MockFile("File 04.jpg"),
            MockFile("File 05.jpg")
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = MockFile("File 03[1].jpg", FileAttributes.Temporary | FileAttributes.ReadOnly);
        var config = new LoadMediaConfig(false, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadMediaFilesFromFilesQueryResult(startFile, neighboringFilesQuery, config);

        Assert.Null(loadMediaFilesTask.StartMediaFile);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        Assert.NotNull(result.StartMediaFile);
        Assert.Equal("File 03.jpg", result.StartMediaFile.FileName);

        var expectedMediaFiles = new[]
        {
            new []{ "File 01.jpg" },
            new []{ "File 02.jpg" },
            new []{ "File 03.jpg" },
            new []{ "File 04.jpg" },
            new []{ "File 05.jpg" },
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }

    private IStorageFile MockFile(string fileName, FileAttributes attributes = FileAttributes.Normal)
    {
        var mock = new Mock<IStorageFile>();
        mock.SetupGet(x => x.Name).Returns(fileName);
        mock.SetupGet(x => x.FileType).Returns(Path.GetExtension(fileName));
        mock.SetupGet(x => x.Attributes).Returns(attributes);
        mock.SetupGet(x => x.Path).Returns(Path.Combine(FolderPath, fileName));
        return mock.Object;
    }

    private IStorageFolder MockFolder(List<IStorageFile> files)
    {
        var folder = Mock.Of<IStorageFolder>();
        fileSystemServiceMock.Setup(x => x.ListFilesAsync(folder)).Returns(Task.FromResult(files));
        return folder;
    }

    private IStorageFolder MockRAWsFolder(List<IStorageFile> files, string name)
    {
        string folderPath = Path.Combine(FolderPath, name);
        var folder = Mock.Of<IStorageFolder>();
        fileSystemServiceMock.Setup(x => x.TryGetFolderAsync(folderPath)).Returns(Task.FromResult(folder)!);
        fileSystemServiceMock.Setup(x => x.ListFilesAsync(folder)).Returns(Task.FromResult(files));
        return folder;
    }

    private IStorageQueryResultBase MockNeighboringFilesQuery(List<IStorageFile> files)
    {
        var neighboringFilesQuery = Mock.Of<IStorageQueryResultBase>();
        fileSystemServiceMock.Setup(x => x.ListFilesAsync(neighboringFilesQuery)).Returns(Task.FromResult(files));
        return neighboringFilesQuery;
    }

    private void AssertMediaFiles(string[][] expected, IList<IMediaFileInfo> mediaFiles)
    {
        Assert.Equal(expected.Length, mediaFiles.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertMediaFile(expected[i], mediaFiles[i]);
        }
    }

    private void AssertMediaFile(string[] expected, IMediaFileInfo actual)
    {
        string errorMessage = $"Expected: [{string.Join(", ", expected)}], " +
            $"Actual: [{string.Join(", ", actual.StorageFiles.Select(f => f.Name))}]";

        Assert.True(expected[0].Equals(actual.FileName), errorMessage);

        int expectedLinkedFilesCount = expected.Length - 1;
        Assert.True(expectedLinkedFilesCount == actual.LinkedStorageFiles.Count, errorMessage);

        for (int i = 1; i < expected.Length; i++)
        {
            Assert.True(expected[i].Equals(actual.LinkedStorageFiles[i - 1].Name), errorMessage);
        }
    }

}
