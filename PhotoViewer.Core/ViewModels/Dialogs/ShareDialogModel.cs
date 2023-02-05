using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public class ShareDialogModel
{
    public IReadOnlyList<IStorageFile> Files { get; }

    public ShareDialogModel(IReadOnlyList<IStorageFile> files)
    {
        Files = files;
    }
}
