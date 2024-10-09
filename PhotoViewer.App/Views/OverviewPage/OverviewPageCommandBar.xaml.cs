using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class OverviewPageCommandBar : CommandBar, IMVVMControl<OverviewPageCommandBarModel>
{
    public OverviewPageCommandBar()
    {
        this.InitializeComponentMVVM();
    }
}
