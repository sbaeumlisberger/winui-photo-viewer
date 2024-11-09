using Essentials.NET.Logging;
using NSubstitute;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Search;
using Xunit;
using Xunit.Abstractions;
using FileAttributes = Windows.Storage.FileAttributes;

namespace PhotoViewer.Test.Services;

public class MediaFilesLoaderServiceTest
{
    private const string FolderPath = @"A:\TestFolder";

    public readonly ICachedImageLoaderService cachedImageLoaderService = Substitute.For<ICachedImageLoaderService>();

    public readonly IFileSystemService fileSystemService = Substitute.For<IFileSystemService>();

    private readonly MediaFilesLoaderService mediaFilesLoaderService;

    private readonly ITestOutputHelper testOutput;

    public MediaFilesLoaderServiceTest(ITestOutputHelper testOutput)
    {
        this.testOutput = testOutput;
        mediaFilesLoaderService = new MediaFilesLoaderService(cachedImageLoaderService, fileSystemService);
    }

    [Fact]
    public async Task LoadMediaFilesFromFolder()
    {
        var files = new List<IStorageFile>
        {
            MockStorageFile("File 01.jpg"),
            MockStorageFile("File 02.jpg"),
            MockStorageFile("File 02.arw"),
            MockStorageFile("File 03.jpg"),
            MockStorageFile("File 04.mp4")
        };
        var folder = MockFolder(files);
        var config = new LoadMediaConfig(false, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadFolder(folder, config);

        Assert.Null(loadMediaFilesTask.PreviewMediaFile);

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
            MockStorageFile("File 01.jpg"),
            MockStorageFile("File 02.jpg"),
            MockStorageFile("File 03.jpg"),
            MockStorageFile("File 04.jpg"),
            MockStorageFile("File 05.jpg")
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[2]; // "File 03.jpg"
        var config = new LoadMediaConfig(false, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);

        Assert.NotNull(loadMediaFilesTask.PreviewMediaFile);
        Assert.Equal("File 03.jpg", loadMediaFilesTask.PreviewMediaFile.FileName);

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
    public async Task Loads1000MediaFilesFromFilesQueryResultInLessThan100ms()
    {
        var files = (await Task.WhenAll(Enumerable.Repeat(1, 1000).Select(_ => CreateStorageFileAsync(Guid.NewGuid() + ".jpg")))).ToList();
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[187];
        var config = new LoadMediaConfig(true, null, false);

        var sw = Stopwatch.StartNew();
        var loadMediaFilesTask = mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);
        var result = await loadMediaFilesTask.WaitForResultAsync();
        sw.Stop();

        testOutput.WriteLine("Took: " + sw.ElapsedMilliseconds + "ms");
        Assert.True(sw.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task LoadMediaFilesFromFilesQueryResult_LinkRAWs()
    {
        var files = new List<IStorageFile>
        {
            MockStorageFile("File 01.jpg"),
            MockStorageFile("File 02.jpg"),
            MockStorageFile("File 03.png"),
            MockStorageFile("File 03.jpg"),
            MockStorageFile("File 03.arw"),
            MockStorageFile("File 04.jpg"),
            MockStorageFile("File 04.arw"),
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[3]; // "File 03.jpg"
        var config = new LoadMediaConfig(true, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);

        Assert.NotNull(loadMediaFilesTask.PreviewMediaFile);
        Assert.Equal("File 03.jpg", loadMediaFilesTask.PreviewMediaFile.FileName);

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
    public async Task UnlinkedRawFileAndXmpFileWithSameNameAreLinked()
    {
        var files = new List<IStorageFile>
        {
            MockStorageFile("File 01.arw"),
            MockStorageFile("File 01.xmp"),
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[0];
        var config = new LoadMediaConfig(true, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);

        Assert.Null(loadMediaFilesTask.PreviewMediaFile);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        Assert.NotNull(result.StartMediaFile);
        AssertMediaFile(new[] { "File 01.arw", "File 01.xmp" }, result.StartMediaFile);

        var expectedMediaFiles = new[]
        {
            new []{ "File 01.arw", "File 01.xmp" }
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }

    [Fact]
    public async Task LoadMediaFilesFromFilesQueryResult_RAWsFolder()
    {
        var files = new List<IStorageFile>
        {
            MockStorageFile("File 01.jpg"),
            MockStorageFile("File 01.arw"),
            MockStorageFile("File 02.jpg"),
            MockStorageFile("File 03.jpg"),
            MockStorageFile("File 04.jpg"),
        };
        var rawFilesInSubfolder = new List<IStorageFile>
        {
            MockStorageFile("File 03.arw"),
            MockStorageFile("File 04.arw"),
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = files[2]; // "File 02.jpg"
        var config = new LoadMediaConfig(true, "RAWs", false);
        MockRAWsFolder(rawFilesInSubfolder, config.RAWsFolderName!);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);

        Assert.NotNull(loadMediaFilesTask.PreviewMediaFile);
        Assert.Equal("File 02.jpg", loadMediaFilesTask.PreviewMediaFile.FileName);

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
            MockStorageFile("File 01.jpg"),
            MockStorageFile("File 02.jpg"),
            MockStorageFile("File 03.jpg"),
            MockStorageFile("File 04.jpg"),
            MockStorageFile("File 05.jpg")
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(files);
        var startFile = MockStorageFile("File 03[1].jpg", FileAttributes.Temporary | FileAttributes.ReadOnly, Path.GetTempPath());
        var config = new LoadMediaConfig(false, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);

        Assert.Null(loadMediaFilesTask.PreviewMediaFile);

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
    public async Task CommandLineActivation()
    {
        var config = new LoadMediaConfig(false, null, false);

        string nonExistingFilePath = Path.Combine(FolderPath, "File 03.jpg");
        var arguments = new[]
        {
            MockStorageFile("File 01.jpg").Path,
            MockStorageFile("File 02.txt").Path,
            nonExistingFilePath,
            MockStorageFile("File 04.png").Path
        };
        fileSystemService.Exists(nonExistingFilePath).Returns(false);
        fileSystemService.TryGetFileAsync(nonExistingFilePath).Returns((IStorageFile?)null);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadFromArguments(arguments, config);

        Assert.NotNull(loadMediaFilesTask.PreviewMediaFile);
        Assert.Equal("File 01.jpg", loadMediaFilesTask.PreviewMediaFile.FileName);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        var expectedMediaFiles = new[]
        {
            new []{ "File 01.jpg" },
            new []{ "File 04.png" },
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }

    [Fact]
    public async Task LoadNeighboringFilesQuery_FallbacksToFilesFromFolderIfTheStartFileIsNotPartOfNeighboringFiles()
    {
        string folderPath = "SomeFolder";
        var startFile = MockStorageFile("Some Other File.jpg", folderPath: folderPath);
        var filesFromFolder = new List<IStorageFile>
        {
            startFile,
            MockStorageFile("File From Folder 01.jpg"),
            MockStorageFile("File From Folder 02.jpg"),
        };
        var folder = Substitute.For<IStorageFolder>();
        fileSystemService.TryGetFolderAsync(folderPath).Returns(folder);
        fileSystemService.ListFilesAsync(folder).Returns(filesFromFolder);
        var neighboringFiles = new List<IStorageFile>
        {
            MockStorageFile("Neighboring File 01.jpg"),
            MockStorageFile("Neighboring File 02.jpg"),
            MockStorageFile("Neighboring File 03.jpg"),
            MockStorageFile("Neighboring File 04.jpg"),
            MockStorageFile("Neighboring File 05.jpg"),
        };
        var neighboringFilesQuery = MockNeighboringFilesQuery(neighboringFiles);

        var config = new LoadMediaConfig(false, null, false);

        var loadMediaFilesTask = mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);

        Assert.NotNull(loadMediaFilesTask.PreviewMediaFile);
        Assert.Equal(startFile.Name, loadMediaFilesTask.PreviewMediaFile.FileName);

        var result = await loadMediaFilesTask.WaitForResultAsync();

        Assert.NotNull(result.StartMediaFile);
        Assert.Equal(startFile.Name, result.StartMediaFile.FileName);

        var expectedMediaFiles = new[]
        {
            new [] { "Some Other File.jpg" },
            new [] { "File From Folder 01.jpg" },
            new [] { "File From Folder 02.jpg" },
        };
        AssertMediaFiles(expectedMediaFiles, result.MediaFiles);
    }

    private IStorageFile MockStorageFile(string fileName, FileAttributes attributes = FileAttributes.Normal, string? folderPath = null)
    {
        string path = Path.Combine(folderPath ?? FolderPath, fileName);
        var mock = Substitute.For<IStorageFile>();
        mock.Name.Returns(fileName);
        mock.FileType.Returns(Path.GetExtension(fileName));
        mock.Attributes.Returns(attributes);
        mock.Path.Returns(path);
        fileSystemService.Exists(path).Returns(true);
        fileSystemService.TryGetFileAsync(path).Returns(mock);
        return mock;
    }

    private string TestFolderPath = TestUtils.CreateTestFolder();

    private async Task<IStorageFile> CreateStorageFileAsync(string fileName)
    {
        var folder = await StorageFolder.GetFolderFromPathAsync(TestFolderPath);
        return await folder.CreateFileAsync(fileName);
    }

    private IStorageFolder MockFolder(List<IStorageFile> files)
    {
        var folder = Substitute.For<IStorageFolder>();
        fileSystemService.ListFilesAsync(folder).Returns(files);
        return folder;
    }

    private IStorageFolder MockRAWsFolder(List<IStorageFile> files, string name)
    {
        string folderPath = Path.Combine(FolderPath, name);
        var folder = Substitute.For<IStorageFolder>();
        fileSystemService.TryGetFolderAsync(folderPath).Returns(folder);
        fileSystemService.ListFilesAsync(folder).Returns(files);
        return folder;
    }

    private IStorageQueryResultBase MockNeighboringFilesQuery(List<IStorageFile> files)
    {
        var neighboringFilesQuery = Substitute.For<IStorageQueryResultBase>();
        fileSystemService.ListFilesAsync(neighboringFilesQuery).Returns(files);
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
