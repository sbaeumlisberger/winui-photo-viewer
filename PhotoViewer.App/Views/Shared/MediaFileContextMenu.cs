using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class MediaFileContextMenuHolder : UserControl, IMVVMControl<MediaFileContextMenuModel>
{
    public MenuFlyout MediaFileContextMenu { get; }

    public MediaFileContextMenuHolder()
    {
        this.InitializeComponentMVVM(updateBindingsAlways: true);
        MediaFileContextMenu = (MenuFlyout)Resources[nameof(MediaFileContextMenu)];
    }
}
