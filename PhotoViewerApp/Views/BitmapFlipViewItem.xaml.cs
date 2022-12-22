using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.Utils;

namespace PhotoViewerApp.Views;

public sealed partial class BitmapFlipViewItem : UserControl, IMVVMControl<BitmapFlipViewItemModel>
{
    private BitmapFlipViewItemModel ViewModel => (BitmapFlipViewItemModel)DataContext;

    public BitmapFlipViewItem()
    {
        this.InitializeMVVM();
    }

}

