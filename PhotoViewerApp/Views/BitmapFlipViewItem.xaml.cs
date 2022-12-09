using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

public sealed partial class BitmapFlipViewItem : UserControl
{
    private BitmapFlipViewItemModel ViewModel => (BitmapFlipViewItemModel)DataContext;

    private MenuFlyout ContextMenu => MediaFileContextMenuHolder.MediaFileContextMenu;

    public BitmapFlipViewItem()
    {
        this.InitializeComponent();
        DataContextChanged += MediaFlipViewItem_DataContextChanged; ;
    }

    private void MediaFlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        this.Bindings.Update();
    }
}

