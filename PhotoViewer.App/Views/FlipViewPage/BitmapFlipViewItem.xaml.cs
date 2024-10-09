using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using Windows.Foundation;

namespace PhotoViewer.App.Views;

public sealed partial class BitmapFlipViewItem : UserControl, IMVVMControl<BitmapFlipViewItemModel>
{
    public BitmapFlipViewItem()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(BitmapFlipViewItemModel viewModel)
    {
        if (viewModel.IsSelected)
        {
            LoadContent(viewModel);
        }
        else
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                LoadContent(viewModel);
            });
        }
    }

    private void LoadContent(BitmapFlipViewItemModel viewModel)
    {
        FindName(nameof(content));
        bitmapViewer.ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;

        viewModel.Subscribe(this, nameof(viewModel.IsSelected), OnIsSelectedChanged);
        viewModel.Subscribe(this, nameof(viewModel.IsDiashowActive), OnIsDiashowActiveChanged);
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

    private void OnIsDiashowActiveChanged()
    {
        if (ViewModel!.IsDiashowActive)
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

            UpdateZoomText();
        }
    }

    private void BitmapViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateZoomText();
    }

    private void UpdateZoomText()
    {
        if (ViewModel?.ImageViewModel.Image is null)
        {
            return;
        }

        double zoomFactor = bitmapViewer.ScrollViewer.ZoomFactor;

        if (zoomFactor == 1 || ViewModel.IsDiashowActive)
        {
            zoomTextBlockContainer.Visibility = Visibility.Collapsed;
            zoomTextBlock.Text = "";
            return;
        }

        Size imageSize = ViewModel.ImageViewModel.Image.SizeInDIPs;

        double rasterizationScale = bitmapViewer.XamlRoot.RasterizationScale;
        Size viewSize = new Size(bitmapViewer.ActualWidth * rasterizationScale, bitmapViewer.ActualHeight * rasterizationScale);

        var imageToViewFactor = Math.Min(Math.Min(viewSize.Width / imageSize.Width, viewSize.Height / imageSize.Height), 1);

        zoomTextBlock.Text = Math.Round(imageToViewFactor * zoomFactor * 100) + " %";
        zoomTextBlockContainer!.Visibility = Visibility.Visible;
    }
}