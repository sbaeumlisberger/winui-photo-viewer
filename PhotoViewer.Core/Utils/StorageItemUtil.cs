using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewer.Core.Utils;

public static class StorageItemUtil
{

    public static bool IsEqual(this IStorageItem storageItemA, IStorageItem storageItemB)
    {
        if (storageItemA is IStorageItem2 storageItem2A)
        {
            return storageItem2A.IsEqual(storageItemB);
        }
        if (!string.IsNullOrEmpty(storageItemA.Path))
        {
            return storageItemA.Path == storageItemB.Path;
        }
        return storageItemA.Equals(storageItemB);
    }

}
