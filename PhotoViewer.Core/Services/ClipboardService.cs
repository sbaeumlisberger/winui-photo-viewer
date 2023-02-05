using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public interface IClipboardService
{
    void CopyStorageItems(IEnumerable<IStorageItem> items);
    void CopyStorageItem(IStorageItem item);
    void CopyText(string text);
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

    public void CopyText(string text)
    {
        DataPackage dp = new DataPackage();
        dp.RequestedOperation = DataPackageOperation.Copy;
        dp.SetText(text);
        Clipboard.SetContent(dp);
        Clipboard.Flush();
    }
}
