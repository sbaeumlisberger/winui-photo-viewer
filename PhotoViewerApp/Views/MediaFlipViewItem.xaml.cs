using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

public sealed partial class MediaFlipViewItem : UserControl
{
    public MediaFlipViewItemModel ViewModel => (MediaFlipViewItemModel)DataContext;

    public MediaFlipViewItem()
    {
        this.InitializeComponent();
        DataContextChanged += MediaFlipViewItem_DataContextChanged;
    }

    private void MediaFlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        this.Bindings.Update();
    }
}
