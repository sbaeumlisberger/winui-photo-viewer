using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class OverviewPageCommandBar : CommandBar
{
    private OverviewPageCommandBarModel ViewModel => (OverviewPageCommandBarModel)DataContext;

    public OverviewPageCommandBar()
    {
        this.InitializeComponent();
    }
}
