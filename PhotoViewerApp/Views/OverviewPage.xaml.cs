using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewerApp.Models;
using PhotoViewerApp.Resources;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System.Linq;

namespace PhotoViewerApp.Views;

[ViewRegistration(typeof(OverviewPageModel))]
public sealed partial class OverviewPage : Page
{
    private OverviewPageModel ViewModel => (OverviewPageModel)DataContext;

    public MenuFlyout ContextMenu => MediaFileContextMenuHolder.MediaFileContextMenu;

    public OverviewPage()
    {
        DataContext = PageModelFactory.CreateOverviewPageModel(App.Current.Window.DialogService);
        this.InitializeMVVM<OverviewPageModel>(InitializeComponent,
            (viewModel) => this.Bindings.Initialize(),
            (viewModel) => this.Bindings.StopTracking());
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        App.Current.Window.Title = Strings.OverviewPage_Title;
    }

    private void GridViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        IMediaFileInfo mediaItem = (IMediaFileInfo)((FrameworkElement)sender).DataContext;
        ViewModel.ShowItem(mediaItem);
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedItems = gridView.SelectedItems.Cast<IMediaFileInfo>().ToList();
    }

    private void GridViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var itemModel = ((FrameworkElement)sender).DataContext;
        if (!ViewModel.SelectedItems.Contains(itemModel))
        {
            gridView.SelectedItem = itemModel;
        }
    }
}
