﻿using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core;

public static class AppData
{
    public const string ApplicationName = "WinUI Photo Viewer";

    public const string ExecutableName = "PhotoViewer.App.exe";

    public static PackageVersion Version => Package.Current.Id.Version;

    public const string MapServiceToken = "vQDj7umE60UMzHG2XfCm~ehfqvBJAFQn6pphOPVbDsQ~ArtM_t2j4AyKdgLIa5iXeftg8bEG4YRYCwhUN-SMXhIK73mnPtCYU4nOF2VtqGiF";

    public static readonly string PublicFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "PhotoViewer");

    public static ApplicationDataContainer DataContainer => ApplicationData.Current.LocalSettings;

    public static IStorageFolder PrivateFolder => ApplicationData.Current.LocalFolder;

    static AppData()
    {
        Directory.CreateDirectory(PublicFolder);
    }
}
