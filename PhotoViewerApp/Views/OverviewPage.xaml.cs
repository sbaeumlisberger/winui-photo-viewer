using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(OverviewPageModel))]
public sealed partial class OverviewPage : Page
{
    private OverviewPageModel ViewModel { get; } = PageModelFactory.CreateOverviewPageModel(App.Current.Window.DialogService);

    public OverviewPage()
    {
        this.InitializeComponent();
    }

    private void GridViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        IMediaFileInfo mediaItem = (IMediaFileInfo)((FrameworkElement)sender).DataContext;
        ViewModel.ShowItem(mediaItem);
    }
}
