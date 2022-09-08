using Microsoft.UI.Xaml.Controls;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.Views;
public sealed partial class MetadataPanel : UserControl
{
    public MetadataPanelModel ViewModel => (MetadataPanelModel)DataContext;

    public MetadataPanel()
    {
        this.InitializeComponent();
    }
}
