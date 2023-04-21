using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Controls;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace PhotoViewer.App.Views;

public sealed partial class CompareView : UserControl, IMVVMControl<CompareViewModel>
{
    private CompareViewModel ViewModel => (CompareViewModel)DataContext;

    private bool viewCangedProgrammatic = false;

    public CompareView()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(CompareViewModel viewModel)
    {
        viewModel.ViewChangeRequested += ViewModel_ViewChangeRequested;
    }

    partial void DisconnectFromViewModel(CompareViewModel viewModel)
    {
        viewModel.ViewChangeRequested -= ViewModel_ViewChangeRequested;
    }

    private void ViewModel_ViewChangeRequested(object? sender, CompareViewModel.ViewState e)
    {
        viewCangedProgrammatic = true;
        bitmapViewer.ScrollViewer.ChangeView(e.HorizontalOffset, e.VerticalOffset, e.ZoomFactor, true);
    }

    private void HideDropDownGlyph(object sender, RoutedEventArgs e)
    {
        if (((ComboBox)sender).FindChild("LayoutRoot") is Grid grid)
        {
            grid.ColumnDefinitions[1].Width = new GridLength(0);
        }
    }

    private void BitmapViewer_ViewChanged(BitmapViewer sender, ScrollViewerViewChangedEventArgs args)
    {
        if (!viewCangedProgrammatic)
        {
            var scrollViewer = sender.ScrollViewer;
            ViewModel.OnViewChangedByUser(scrollViewer.ZoomFactor, scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
        }
        else
        {
            viewCangedProgrammatic = false;
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        await DeleteStoryboard.RunAsync();
        await ViewModel.DeleteCommand.ExecuteAsync(null);
        bitmapViewer.Opacity = 1;
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is IBitmapFileInfo value)
        {
            ViewModel.SelectedBitmapFile = value;
        }
        else 
        {
            ((ComboBox)sender).SelectedValue = ViewModel.SelectedBitmapFile;
        }
    }
}
