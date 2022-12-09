using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerCore.ViewModels;
using Windows.System;

namespace PhotoViewerApp.Views;

public sealed partial class MediaFileContextMenuHolder : UserControl
{
    public MediaFileContextMenuModel ViewModel => (MediaFileContextMenuModel)DataContext;

    public MenuFlyout MediaFileContextMenu { get; }

    public MediaFileContextMenuHolder()
    {
        this.InitializeMVVM<MediaFileContextMenuModel>(InitializeComponent, 
            (viewModel) => this.Bindings.Update(),
            (viewModel) => this.Bindings.StopTracking());
        MediaFileContextMenu = (MenuFlyout)Resources[nameof(MediaFileContextMenu)];
    }
}
