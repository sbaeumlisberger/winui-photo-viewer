﻿using Windows.ApplicationModel;
using Windows.Storage;

namespace PhotoViewer.Core;

public static class AppData
{
    public const string ApplicationName = "Universe Photos Dev";

    public const string ExecutableName = "PhotoViewer.App.exe";

    public static PackageVersion Version => Package.Current.Id.Version;

    public static readonly string PublicFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "PhotoViewer");

    public static readonly string PrivateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoViewer");

    public static ApplicationDataContainer DataContainer => ApplicationData.Current.LocalSettings;

    static AppData()
    {
        Directory.CreateDirectory(PublicFolder);
        Directory.CreateDirectory(PrivateFolder);
    }
}
