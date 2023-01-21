using Microsoft.UI.Xaml.Controls;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.Views;
public sealed partial class ItemWithCount : UserControl
{
    public ItemWithCountModel ViewModel => (ItemWithCountModel)DataContext;

    public ItemWithCount()
    {
        this.InitializeComponent();
    }

}
