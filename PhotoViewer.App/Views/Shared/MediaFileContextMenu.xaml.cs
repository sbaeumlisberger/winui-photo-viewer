using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class MediaFileContextMenuHolder : UserControl, IMVVMControl<MediaFileContextMenuModel>
{
    public MenuFlyout MediaFileContextMenu { get; }

    public MediaFileContextMenuHolder()
    {
        this.InitializeComponentMVVM();
        MediaFileContextMenu = (MenuFlyout)Resources[nameof(MediaFileContextMenu)];
    }
}
