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
        await JsonSerializer.SerializeAsync(stream, settings);
    }

    public async Task ExportSettingsAsync(IStorageFile file)
    {
        var settings = await LoadSettingsAsync();
        using var stream = File.Open(file.Path, FileMode.Create);
        var jsonOptions = new JsonSerializerOptions() 
        { 
            WriteIndented = true, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        };
        await JsonSerializer.SerializeAsync(stream, settings, jsonOptions);
    }

    public async Task<ApplicationSettings> ImportSettingsAsync(IStorageFile file)
    {
        using var stream = File.OpenRead(file.Path);
        var settingsToImport = await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream).ConfigureAwait(false);
        await SaveSettingsAsync(settingsToImport ?? throw new Exception("Invalid settings")); // TODO test
        return settingsToImport;
    }

}
