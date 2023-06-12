using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using WinRT;

namespace PhotoViewer.Core.Utils;

public static class StorageItemUtil
{

    public static bool IsSameFile(this IStorageItem storageItemA, IStorageItem storageItemB)
    {
        if (!string.IsNullOrEmpty(storageItemA.Path))
        {
            return storageItemA.Path == storageItemB.Path;
        }
        if (storageItemA is IStorageItem2 storageItem2A)
        {
            return storageItem2A.IsEqual(storageItemB);
        }
        return storageItemA.Equals(storageItemB);
    }

    public static Task<StorageFolder> GetParentAsync(this IStorageItem storageItem)
    {
        return ((IStorageItem2)storageItem).GetParentAsync().AsTask();
    }

}
