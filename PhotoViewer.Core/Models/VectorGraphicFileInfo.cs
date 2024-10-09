using Windows.Foundation;
using Windows.Storage;

namespace PhotoViewer.Core.Models;

public interface IVectorGraphicFileInfo : IMediaFileInfo
{
}

internal class VectorGraphicFileInfo : MediaFileInfoBase, IVectorGraphicFileInfo
{

    public static readonly IReadOnlySet<string> SupportedFileExtensions = new HashSet<string>() { ".svg" };

    public VectorGraphicFileInfo(IStorageFile file) : base(file)
    {
    }

    public override Task<Size> GetSizeInPixelsAsync()
    {
        return Task.FromResult(Size.Empty);
    }
}
