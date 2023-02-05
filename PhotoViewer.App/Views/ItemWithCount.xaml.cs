using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;
public sealed partial class ItemWithCount : UserControl
{
    public ItemWithCountModel ViewModel => (ItemWithCountModel)DataContext;

    public ItemWithCount()
    {
        this.InitializeComponent();
    }

}
