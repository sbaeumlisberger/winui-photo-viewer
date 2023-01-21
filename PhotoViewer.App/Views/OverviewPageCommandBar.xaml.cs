using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

public sealed partial class OverviewPageCommandBar : CommandBar
{
    private OverviewPageCommandBarModel ViewModel => (OverviewPageCommandBarModel)DataContext;

    public OverviewPageCommandBar()
    {
        this.InitializeComponent();
    }
}
