using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Notifications;

namespace PhotoViewerCore.Services;

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
        var storageFile = file as StorageFile ?? await file.CopyAsync(ApplicationData.Current.LocalFolder, "lockscreenBackground" + file.FileType, NameCollisionOption.ReplaceExisting);
        bool success = await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(storageFile);
        if (!success)
        {
            throw new Exception("Background image could not be set successfully.");
        }
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
        var storageFile = file as StorageFile ?? await file.CopyAsync(ApplicationData.Current.LocalFolder, "desktopBackground" + file.FileType, NameCollisionOption.ReplaceExisting);
        bool success = await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(storageFile);
        if (!success)
        {
            throw new Exception("Background image could not be set successfully.");
        }
    }

}
