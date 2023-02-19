using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public interface ISettingsService
{
    ApplicationSettings LoadSettings();

    void SaveSettings(ApplicationSettings settings);

    void ExportSettings(IStorageFile file);

    ApplicationSettings ImportSettings(IStorageFile file);
}

public class SettingsService : ISettingsService
{
    public const string SettingsFileName = "settings.ini";

    private readonly string settingsFilePath;

    public SettingsService(string? appDataFolder = null) 
    {
        settingsFilePath = Path.Combine(appDataFolder ?? AppData.LocalFolder, "settings.ini");
    }

    public ApplicationSettings LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            var fileContent = File.ReadAllText(settingsFilePath);
            return ApplicationSettings.Deserialize(fileContent);
        }
        else
        {
            return new ApplicationSettings();
        }
    }

    public void SaveSettings(ApplicationSettings settings)
    {
        File.WriteAllText(settingsFilePath, settings.Serialize());
    }

    public void ExportSettings(IStorageFile file)
    {
        var settings = LoadSettings();
        File.WriteAllText(file.Path, settings.Serialize());
    }

    public ApplicationSettings ImportSettings(IStorageFile file)
    {
        var fileContent = File.ReadAllText(file.Path);
        var settingsToImport = ApplicationSettings.Deserialize(fileContent);
        SaveSettings(settingsToImport ?? throw new Exception("Invalid settings")); // TODO test
        return settingsToImport;
    }

}
