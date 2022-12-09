using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace PhotoViewerCore.Services;

public interface IClipboardService
{
    void CopyStorageItems(IEnumerable<IStorageItem> items);
    void CopyStorageItem(IStorageItem item);
}

public class ClipboardService : IClipboardService
{
    public void CopyStorageItems(IEnumerable<IStorageItem> items)
    {
        DataPackage dp = new DataPackage();
        dp.RequestedOperation = DataPackageOperation.Copy;
        dp.SetStorageItems(items);
        Clipboard.SetContent(dp);
        Clipboard.Flush();
    }

    public void CopyStorageItem(IStorageItem item)
    {
        CopyStorageItems(new[] { item });
    }
}
