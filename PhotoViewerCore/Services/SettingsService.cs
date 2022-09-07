using PhotoViewerCore.Models;
using System.Text.Json;
using Windows.Storage;

namespace PhotoViewerCore.Services;

public interface ISettingsService
{
    Task<ApplicationSettings> LoadSettingsAsync();

    Task SaveSettingsAsync(ApplicationSettings settings);

    Task ExportSettingsAsync(IStorageFile file);

    Task<ApplicationSettings> ImportSettingsAsync(IStorageFile file);
}

public class SettingsService : ISettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");

    public async Task<ApplicationSettings> LoadSettingsAsync()
    {
        if (File.Exists(SettingsFilePath))
        {
            using var stream = File.OpenRead(SettingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream).ConfigureAwait(false);
            return settings ?? new ApplicationSettings();
        }
        else
        {
            return new ApplicationSettings();
        }
    }

    public async Task SaveSettingsAsync(ApplicationSettings settings)
    {
        using var stream = File.OpenWrite(SettingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings);
    }

    public async Task ExportSettingsAsync(IStorageFile file)
    {
        var settings = await LoadSettingsAsync();
        using var stream = File.OpenWrite(file.Path);
        var options = new JsonSerializerOptions() { WriteIndented = true };
        await JsonSerializer.SerializeAsync(stream, settings, options);
    }

    public async Task<ApplicationSettings> ImportSettingsAsync(IStorageFile file)
    {
        using var stream = File.OpenRead(file.Path);
        var settingsToImport = await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream).ConfigureAwait(false);
        await SaveSettingsAsync(settingsToImport ?? throw new Exception("Invalid settings")); // TODO test
        return settingsToImport;
    }

}
