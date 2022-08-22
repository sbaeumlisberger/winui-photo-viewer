using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public sealed class FolderPickerModel
{
    public PickerLocationId SuggestedStartLocation { get; set; }
    public StorageFolder? Folder { get; set; }
}
