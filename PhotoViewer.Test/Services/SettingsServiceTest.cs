using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.Diagnostics;
using Windows.Storage;
using Xunit;

namespace PhotoViewer.Test.Services;

public class SettingsServiceTest : IDisposable
{
    private const string ExampleSettingsFileContent = """
        Theme=Dark
        ShowDeleteAnimation=False
        AutoOpenMetadataPanel=True
        AutoOpenDetailsBar=True
        DiashowTime=00:00:05
        LinkRawFiles=True
        RawFilesFolderName=RAWs
        DeleteLinkedFilesOption=Yes
        IncludeVideos=True
        IsDebugLogEnabled=True

        """;

    private readonly string tempDirectory;
    private readonly string settingsFilePath;
    private readonly SettingsService settingsService;

    public SettingsServiceTest()
    {
        tempDirectory = Directory.CreateTempSubdirectory(GetType().FullName).FullName;
        settingsFilePath = Path.Combine(tempDirectory, SettingsService.SettingsFileName);
        settingsService = new SettingsService(tempDirectory);
    }

    public void Dispose()
    {
        Directory.Delete(tempDirectory, true);
    }

    [Fact]
    public void LoadSettings()
    {
        File.WriteAllText(settingsFilePath, ExampleSettingsFileContent);

        var stopwatch = Stopwatch.StartNew();

        var settings = settingsService.LoadSettings();

        stopwatch.Stop();

        AssertExampleSettings(settings);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 10);
    }

    [Fact]
    public void LoadSettings_FileNotExist()
    {
        var settings = settingsService.LoadSettings();

        Assert.NotNull(settings);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.True(settings.ShowDeleteAnimation);
        Assert.False(settings.AutoOpenMetadataPanel);
        Assert.False(settings.AutoOpenDetailsBar);
        Assert.Equal(TimeSpan.FromSeconds(3), settings.DiashowTime);
        Assert.True(settings.LinkRawFiles);
        Assert.Equal("RAWs", settings.RawFilesFolderName);
        Assert.Equal(DeleteLinkedFilesOption.Ask, settings.DeleteLinkedFilesOption);
        Assert.True(settings.IncludeVideos);
    }

    [Fact]
    public void LoadSettings_SavedBefore()
    {
        settingsService.SaveSettings(CreateExampleSettings());

        var settings = settingsService.LoadSettings();

        AssertExampleSettings(settings);
    }

    [Fact]
    public void SaveSettings()
    {
        var settings = CreateExampleSettings();

        settingsService.SaveSettings(settings);

        Assert.True(File.Exists(settingsFilePath));
        string fileContent = File.ReadAllText(settingsFilePath);
        Assert.Equal(ExampleSettingsFileContent, fileContent);
    }


    [Fact]
    public async Task ExportSettings()
    {
        File.WriteAllText(settingsFilePath, ExampleSettingsFileContent);
        string filePath = Path.Combine(tempDirectory, "exported-settings.ini");
        File.WriteAllText(filePath, "");
        var file = await StorageFile.GetFileFromPathAsync(filePath);

        settingsService.ExportSettings(file);

        Assert.True(File.Exists(file.Path));
        string fileContent = File.ReadAllText(file.Path);
        Assert.Equal(ExampleSettingsFileContent, fileContent);
    }

    [Fact]
    public async Task ImportSettings()
    {
        string filePath = Path.Combine(tempDirectory, "import-settings.ini");
        File.WriteAllText(filePath, ExampleSettingsFileContent);
        var file = await StorageFile.GetFileFromPathAsync(filePath);

        var settings = settingsService.ImportSettings(file);

        AssertExampleSettings(settings);
        AssertExampleSettings(settingsService.LoadSettings());
    }

    [Fact]
    public async Task ImportSettings_EmptyFile()
    {
        string filePath = Path.Combine(tempDirectory, "import-settings.ini");
        File.WriteAllText(filePath, "");
        var file = await StorageFile.GetFileFromPathAsync(filePath);

        Assert.ThrowsAny<Exception>(() => settingsService.ImportSettings(file));
    }

    private ApplicationSettings CreateExampleSettings()
    {
        var settings = new ApplicationSettings();
        settings.Theme = AppTheme.Dark;
        settings.ShowDeleteAnimation = false;
        settings.AutoOpenMetadataPanel = true;
        settings.AutoOpenDetailsBar = true;
        settings.DiashowTime = TimeSpan.FromSeconds(5);
        settings.DeleteLinkedFilesOption = DeleteLinkedFilesOption.Yes;
        settings.IsDebugLogEnabled = true;
        return settings;
    }

    private void AssertExampleSettings(ApplicationSettings settings)
    {
        Assert.NotNull(settings);
        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.False(settings.ShowDeleteAnimation);
        Assert.True(settings.AutoOpenMetadataPanel);
        Assert.True(settings.AutoOpenDetailsBar);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.DiashowTime);
        Assert.True(settings.LinkRawFiles);
        Assert.Equal("RAWs", settings.RawFilesFolderName);
        Assert.Equal(DeleteLinkedFilesOption.Yes, settings.DeleteLinkedFilesOption);
        Assert.True(settings.IncludeVideos);
        Assert.True(settings.IsDebugLogEnabled);
    }

}