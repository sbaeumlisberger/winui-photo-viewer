using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.ComponentModel;

namespace PhotoViewer.App.Views;

public sealed partial class BitmapFlipViewItem : UserControl, IMVVMControl<BitmapFlipViewItemModel>
{
    public BitmapFlipViewItem()
    {
        this.InitializeComponentMVVM();
        bitmapViewer.ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;
    }

    partial void ConnectToViewModel(BitmapFlipViewItemModel viewModel)
    {
        viewModel.Subscribe(this, nameof(viewModel.IsSelected), OnIsSelectedChanged);
    }

    partial void DisconnectFromViewModel(BitmapFlipViewItemModel viewModel)
    {
        viewModel.UnsubscribeAll(this);
    }

    private void OnIsSelectedChanged()
    {
        if (!ViewModel!.IsSelected)
        {
            bitmapViewer.ScrollViewer.ChangeView(0, 0, 1);
        }
    }

    private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            float uiScaleFactor = 1.0f / bitmapViewer.ScrollViewer.ZoomFactor;

            ViewModel.CropImageToolModel.UIScaleFactor = uiScaleFactor;

            if (ViewModel.PeopleTagToolModel != null)
            {
                ViewModel.PeopleTagToolModel.UIScaleFactor = uiScaleFactor;
            }            
        }
    }

}

