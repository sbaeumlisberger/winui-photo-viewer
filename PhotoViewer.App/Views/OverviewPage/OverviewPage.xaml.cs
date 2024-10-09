using Essentials.NET;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Resources;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(OverviewPageModel))]
public sealed partial class OverviewPage : Page, IMVVMControl<OverviewPageModel>
{
    private readonly PrintService printService = new PrintService(App.Current.Window);

    private PrintRegistration? printRegistration;

    private bool isUpdatingSelection = false;

    public OverviewPage()
    {
        DataContext = App.Current.ViewModelFactory.CreateOverviewPageModel();
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(OverviewPageModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    partial void DisconnectFromViewModel(OverviewPageModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        viewModel.Cleanup();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(OverviewPageModel.SelectedItems):
                UpdateSelection();
                break;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel!.OnNavigatedTo();

        printRegistration = printService.RegisterForPrinting(() =>
        {
            var files = ViewModel!.SelectedItems.ToList();
            return new PhotoPrintJob(files);
        });
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        if (printRegistration != null)
        {
            printService.Unregister(printRegistration);
        }
    }

    private void UpdateSelection()
    {
        if (!gridView.SelectedItems.SequenceEqual(ViewModel!.SelectedItems))
        {
            isUpdatingSelection = true;
            gridView.SelectedItems.ReplaceAll(ViewModel!.SelectedItems);
            isUpdatingSelection = false;
        }
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdatingSelection)
        {
            ViewModel!.SelectedItems = gridView.SelectedItems.Cast<IMediaFileInfo>().ToList();
        }
    }

    private void GridView_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    {
        if (args.ItemContainer?.ContentTemplateRoot is { } element)
        {
            element.Visibility = Visibility.Collapsed;
        }
    }

    private void OverviewItemBorder_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender.DataContext is IMediaFileInfo mediaFile)
        {
            var overviewItem = (OverviewItem)((Border)sender).Child;
            overviewItem.DataContext = ViewModel!.GetItemModel(mediaFile);
            sender.Visibility = Visibility.Visible;
        }
    }

    private void OverviewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var itemModel = (OverviewItemModel)((FrameworkElement)sender).DataContext;
        ViewModel!.ShowItem(itemModel.MediaFile);
    }

    private void OverviewItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        var itemModel = (OverviewItemModel)((FrameworkElement)sender).DataContext;
        if (!ViewModel!.SelectedItems.Contains(itemModel.MediaFile))
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
