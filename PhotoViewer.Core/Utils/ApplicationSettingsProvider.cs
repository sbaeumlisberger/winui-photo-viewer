using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Utils;

public static class ApplicationSettingsProvider
{
    private static ApplicationSettings? settings;

    public static ApplicationSettings GetSettings()
    {
        return settings ?? throw new InvalidOperationException("Settings not set");
    }

    public static void SetSettings(ApplicationSettings settings)
    {
        ApplicationSettingsProvider.settings = settings;
    }

}
