using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels.Shared;

namespace PhotoViewer.App.Views.Shared;

public sealed partial class SortAppBarButton : AppBarButton, IMVVMControl<SortMenuModel>
{
    public SortAppBarButton()
    {
        this.InitializeComponentMVVM();
    }
}
