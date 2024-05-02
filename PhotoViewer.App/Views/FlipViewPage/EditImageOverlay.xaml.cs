using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

public sealed partial class EditImageOverlay : UserControl
{
    public EditImageOverlayModel ViewModel => (EditImageOverlayModel)DataContext;

    public EditImageOverlay()
    {
        this.InitializeComponent();
    }
}
