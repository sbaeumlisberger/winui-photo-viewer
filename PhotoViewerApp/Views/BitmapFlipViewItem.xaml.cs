using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

public sealed partial class BitmapFlipViewItem : UserControl
{
    private BitmapFlipViewItemModel ViewModel => (BitmapFlipViewItemModel)DataContext;

    public BitmapFlipViewItem()
    {
        this.InitializeMVVM<BitmapFlipViewItemModel>(InitializeComponent,
            connectToViewModel: viewModel => this.Bindings.Update(),
            disconnectFromViewModel: viewModel => this.Bindings.StopTracking());
    }


}

