using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class MediaFileContextMenuHolder : UserControl, IMVVMControl<MediaFileContextMenuModel>
{
    public MediaFileContextMenuModel ViewModel => (MediaFileContextMenuModel)DataContext;

    public MenuFlyout MediaFileContextMenu { get; }

    public MediaFileContextMenuHolder()
    {
        this.InitializeMVVM();
        MediaFileContextMenu = (MenuFlyout)Resources[nameof(MediaFileContextMenu)];
    }
}
