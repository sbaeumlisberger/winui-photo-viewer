using Essentials.NET.Logging;
using Windows.Storage;
using Windows.System.UserProfile;

namespace PhotoViewer.Core.Services;

public interface IPersonalizationService
{
    bool CanSetLockScreenBackground { get; }
    bool CanSetDesktopBackground { get; }
    bool IsFileExtensionSupported(string fileExtension);
    Task SetLockScreenBackgroundAsync(IStorageFile file);
    Task SetDesktopBackgroundAsync(IStorageFile file);
}

public class PersonalizationService : IPersonalizationService
{

    public static readonly IEnumerable<string> SupportedFileTypes = new HashSet<string>() { ".jpe", ".jpeg", ".jpg", ".jfif", ".tiff", ".tif", ".bmp", ".dib", ".gif", ".png", ".jxr", ".wdp" };

    public bool CanSetLockScreenBackground => UserProfilePersonalizationSettings.IsSupported();
    public bool CanSetDesktopBackground => UserProfilePersonalizationSettings.IsSupported();

    public bool IsFileExtensionSupported(string fileExtension) => SupportedFileTypes.Contains(fileExtension.ToLower());

    public async Task SetLockScreenBackgroundAsync(IStorageFile file)
    {
        if (!UserProfilePersonalizationSettings.IsSupported())
        {
            throw new PlatformNotSupportedException();
        }
        if (!IsFileExtensionSupported(file.FileType))
        {
            throw new ArgumentException("The given file type is not supported.");
        }
        var appDataFolder = ApplicationData.Current.LocalFolder;
        var uniqueFile = await file.CopyAsync(appDataFolder, "lockscreenBackground-" + Guid.NewGuid() + file.FileType);
        bool success = await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(uniqueFile);
        if (!success)
        {
            throw new Exception("Background image could not be set successfully.");
        }
        TryCleanupOldFilesAsync(appDataFolder, "lockscreenBackground-", uniqueFile);
    }

    public async Task SetDesktopBackgroundAsync(IStorageFile file)
    {
        if (!UserProfilePersonalizationSettings.IsSupported())
        {
            throw new PlatformNotSupportedException();
        }
        if (!IsFileExtensionSupported(file.FileType))
        {
            throw new ArgumentException("The given file type is not supported.");
        }
        var appDataFolder = ApplicationData.Current.LocalFolder;
        var uniqueFile = await file.CopyAsync(appDataFolder, "desktopBackground-" + Guid.NewGuid() + file.FileType);
        bool success = await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(uniqueFile);
        if (!success)
        {
            throw new Exception("Background image could not be set successfully.");
        }
        TryCleanupOldFilesAsync(appDataFolder, "desktopBackground-", uniqueFile);
    }

    private async void TryCleanupOldFilesAsync(StorageFolder privateFolder, string prefix, StorageFile currentFile)
    {
        try
        {
            var files = await privateFolder.GetFilesAsync();

            foreach (var file in files)
            {
                if (file.Name.StartsWith(prefix) && file.Name != currentFile.Name)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to cleanup old personalization files.", ex);
        }
    }

}
