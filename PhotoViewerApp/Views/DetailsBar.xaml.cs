using Microsoft.UI.Xaml.Controls;
using PhotoViewerApp.ViewModels;


namespace PhotoViewerApp.Views;

public sealed partial class DetailsBar : UserControl
{

    private DetailsBarModel ViewModel => (DetailsBarModel)DataContext;

    public DetailsBar()
    {
        this.InitializeComponent();
    }
}
