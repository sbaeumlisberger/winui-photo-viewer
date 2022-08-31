using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Models;
internal class VectorGraphicFileInfo : MediaFileInfoBase
{

    public static readonly IReadOnlySet<string> SupportedFileExtensions = new HashSet<string>() { ".svg" };

    public VectorGraphicFileInfo(IStorageFile file) : base(file)
    {
    }

}
