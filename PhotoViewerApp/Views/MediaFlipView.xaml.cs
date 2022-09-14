﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using PhotoVieweApp.Utils;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System.ComponentModel;
using Windows.Foundation;

namespace PhotoViewerApp.Views;
public sealed partial class MediaFlipView : UserControl
{
    private MediaFlipViewModel ViewModel => (MediaFlipViewModel)DataContext;

    public MediaFlipView()
    {
        this.InitializeComponent();
        this.WhenDataContextSet(() =>
        {
            ViewModel.PropertyChanged += FlipViewModel_PropertyChanged;
            UpdateWindowTitle();
        });
    }

    private void FlipViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedItemModel))
        {
            UpdateWindowTitle();
        }
    }

    private void UpdateWindowTitle()
    {
        App.Current.Window.Title = ViewModel.SelectedItemModel?.MediaItem.Name ?? "";
    }

    private void FlipView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ViewModel.IsDiashowActive)
        {
            flipView.ShowAttachedFlyout(args);
        }
    }

    private void FlipView_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var window = App.Current.Window;
        var windowCenterX = window.Bounds.Left + window.Bounds.Width / 2;

        if (e.GetPosition(flipView).X > windowCenterX)
        {
            ViewModel.Diashow_SelectNext();
        }
        else 
        {
            ViewModel.Diashow_SelectPrevious();
        }
    }
}
