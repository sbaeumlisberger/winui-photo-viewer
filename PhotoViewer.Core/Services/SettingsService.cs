using PhotoViewerCore.Models;
using System.Text.Encodings.Web;
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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
        using var stream = File.Open(SettingsFilePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions).ConfigureAwait(false);
    }

    public async Task ExportSettingsAsync(IStorageFile file)
    {
        var settings = await LoadSettingsAsync().ConfigureAwait(false);
        using var stream = File.Open(file.Path, FileMode.Create);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions).ConfigureAwait(false);
    }

    public async Task<ApplicationSettings> ImportSettingsAsync(IStorageFile file)
    {
        using var stream = File.OpenRead(file.Path);
        var settingsToImport = await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream, JsonOptions).ConfigureAwait(false);
        await SaveSettingsAsync(settingsToImport ?? throw new Exception("Invalid settings")).ConfigureAwait(false); // TODO test
        return settingsToImport;
    }

}
