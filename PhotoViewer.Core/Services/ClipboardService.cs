using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.Services;

public interface IClipboardService
{
    void CopyStorageItems(IEnumerable<IStorageItem> items);
    void CopyStorageItem(IStorageItem item);
    void CopyBitmapFile(IStorageFile bitmapFile);
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
        CopyStorageItems([item]);
    }

    public void CopyBitmapFile(IStorageFile bitmapFile)
    {
        DataPackage dp = new DataPackage();
        dp.RequestedOperation = DataPackageOperation.Copy;
        dp.SetBitmap(RandomAccessStreamReference.CreateFromFile(bitmapFile));
        dp.SetStorageItems([bitmapFile]);
        Clipboard.SetContent(dp);
        Clipboard.Flush();
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
