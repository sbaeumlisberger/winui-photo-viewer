using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

public sealed partial class FlipViewPageCommandBar : CommandBar
{
    private FlipViewPageCommandBarModel ViewModel => (FlipViewPageCommandBarModel)DataContext;

    public FlipViewPageCommandBar()
    {
        this.InitializeComponent();
    }
}
