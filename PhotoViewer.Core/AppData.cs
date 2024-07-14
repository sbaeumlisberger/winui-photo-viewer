using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core;

public static class AppData
{
    public const string ApplicationName = "Universe Photos";

    public const string ExecutableName = "PhotoViewer.App.exe";

    public static PackageVersion Version => Package.Current.Id.Version;

    public static string MapServiceToken => CompileTimeConstants.BingMapsKey;

    public static readonly string PublicFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "PhotoViewer");

    public static ApplicationDataContainer DataContainer => ApplicationData.Current.LocalSettings;

    public static IStorageFolder PrivateFolder => ApplicationData.Current.LocalFolder;

    static AppData()
    {
        Directory.CreateDirectory(PublicFolder);
    }
}
