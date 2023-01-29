﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using System.ComponentModel;

namespace PhotoViewer.App.Views;

public sealed partial class BitmapFlipViewItem : UserControl, IMVVMControl<BitmapFlipViewItemModel>
{
    private BitmapFlipViewItemModel ViewModel => (BitmapFlipViewItemModel)DataContext;

    public BitmapFlipViewItem()
    {
        this.InitializeMVVM(ConnectToViewModel, DisconnectFromViewModel);
        bitmapViewer.ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;
    }

    private void ConnectToViewModel(BitmapFlipViewItemModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }
    private void DisconnectFromViewModel(BitmapFlipViewItemModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsSelected) && !ViewModel.IsSelected)
        {
            bitmapViewer.ScrollViewer.ChangeView(0, 0, 1);
        }
    }

    private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (ViewModel.PeopleTagToolModel != null)
        {
            ViewModel.PeopleTagToolModel.UIScaleFactor = 1 / bitmapViewer.ScrollViewer.ZoomFactor;
        }
    }
}
