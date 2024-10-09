using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class FlipViewPageCommandBar : CommandBar, IMVVMControl<FlipViewPageCommandBarModel>
{
    public FlipViewPageCommandBar()
    {
        this.InitializeComponentMVVM();
    }
}
