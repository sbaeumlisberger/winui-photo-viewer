using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class OverviewPageCommandBar : CommandBar, IMVVMControl<OverviewPageCommandBarModel>
{
    public OverviewPageCommandBar()
    {
        this.InitializeComponentMVVM();
    }
}
