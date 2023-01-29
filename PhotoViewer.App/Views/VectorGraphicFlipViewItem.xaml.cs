using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.ViewModels;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace PhotoViewer.App.Views;

public sealed partial class VectorGraphicFlipViewItem : UserControl
{
    private VectorGraphicFlipViewItemModel? ViewModel { get; set; }

    public VectorGraphicFlipViewItem()
    {
        this.InitializeComponent();

        ScrollViewerHelper.EnableAdvancedZoomBehaviour(scrollViewer);

        DataContextChanged += VectorGraphicFlipViewItem_DataContextChanged;
    }

    private async void VectorGraphicFlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        ViewModel = (VectorGraphicFlipViewItemModel)DataContext;

        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (ViewModel.Content is string svg)
            {
                await ShowSvgAsync(svg);
            }
        }
    }

    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Content))
        {
            if (ViewModel!.Content is string svg)
            {
                await ShowSvgAsync(svg);
            }
        }
    }

    private async Task ShowSvgAsync(string svg)
    {
        await webView.EnsureCoreWebView2Async();
        webView.NavigateToString(svg);
    }

    private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        string zoom = scrollViewer.ZoomFactor.ToString(CultureInfo.InvariantCulture);
        string translateX = (-(scrollViewer.HorizontalOffset - (scrollViewer.ExtentWidth - scrollViewer.ViewportWidth) / 2)).ToString(CultureInfo.InvariantCulture);
        string translateY = (-(scrollViewer.VerticalOffset - (scrollViewer.ExtentHeight - scrollViewer.ViewportHeight) / 2)).ToString(CultureInfo.InvariantCulture);
        await webView.ExecuteScriptAsync($"document.getElementsByTagName(\"svg\")[0].style.transform = \"scale({zoom}) translate({translateX}px, {translateY}px)\"");
    }
}
