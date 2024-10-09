using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class DeleteLinkedFilesDialogModel : ViewModelBase
{
    public bool IsYes { get; set; } = false;
    public bool IsRemember { get; set; } = false;
}
