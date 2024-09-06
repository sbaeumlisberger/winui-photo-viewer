using PhotoViewer.Core.Models;
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

    public SettingsService(string? folder = null)
    {
        settingsFilePath = Path.Combine(folder ?? AppData.PublicFolder, "settings.ini");
    }

    public ApplicationSettings LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            var fileContent = File.ReadAllLines(settingsFilePath);
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
        var fileContent = File.ReadAllLines(file.Path);
        var settingsToImport = ApplicationSettings.Deserialize(fileContent);
        SaveSettings(settingsToImport);
        return settingsToImport;
    }

}
