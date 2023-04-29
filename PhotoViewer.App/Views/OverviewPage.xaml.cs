using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PhotoVieweApp.Utils;
using PhotoViewer.App.Models;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core;
using PhotoViewer.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using PhotoViewer.App.Services;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(OverviewPageModel))]
public sealed partial class OverviewPage : Page, IMVVMControl<OverviewPageModel>
{
    private OverviewPageModel ViewModel => (OverviewPageModel)DataContext;

    private readonly PrintService printService = new PrintService(App.Current.Window);

    private PrintRegistration? printRegistration;

    public OverviewPage()
    {
        DataContext = ViewModelFactory.Instance.CreateOverviewPageModel();
        this.InitializeComponentMVVM();
    }

    partial void DisconnectFromViewModel(OverviewPageModel viewModel)
    {
        viewModel.Cleanup();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        App.Current.Window.Title = Strings.OverviewPage_Title + " - WinUI Photo Viewer"; // TODO use message

        printRegistration = printService.RegisterForPrinting(() => new PhotoPrintJob(ViewModel.SelectedItems.Select(mediaFile => mediaFile.StorageFile).ToList()));
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        if (printRegistration != null)
        {
            printService.Unregister(printRegistration);
        }
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedItems = gridView.SelectedItems.Cast<IMediaFileInfo>().ToList();
    }
    private void OverviewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender.DataContext is IMediaFileInfo mediaFile)
        {
            sender.DataContext = ViewModel.GetItemModel(mediaFile);
        }
    }

    private void OverviewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var itemModel = (OverviewItemModel)((FrameworkElement)sender).DataContext;
        ViewModel.ShowItem(itemModel.MediaFile);
    }

    private void OverviewItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        var itemModel = (OverviewItemModel)((FrameworkElement)sender).DataContext;
        if (!ViewModel.SelectedItems.Contains(itemModel.MediaFile))
        {
            gridView.SelectedItem = itemModel.MediaFile;
        }
        gridView.ShowAttachedFlyout(args);
    }

    private string FormatSelectionInfo(IReadOnlyList<IMediaFileInfo> items, IReadOnlyList<IMediaFileInfo> selectedItems) 
    {
        return string.Format(Strings.OverviewPage_SelectionInfo, items.Count, selectedItems.Count);
    }
}
