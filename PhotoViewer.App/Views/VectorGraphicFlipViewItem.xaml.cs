using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoVieweApp.Utils;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace PhotoViewer.App.Views;

public sealed partial class VectorGraphicFlipViewItem : UserControl, IMVVMControl<VectorGraphicFlipViewItemModel>
{
    private VectorGraphicFlipViewItemModel ViewModel  => DataContext as VectorGraphicFlipViewItemModel;

    public VectorGraphicFlipViewItem()
    {
        this.InitializeComponentMVVM(updateBindingsAlways: true);
        ScrollViewerHelper.EnableAdvancedZoomBehaviour(scrollViewer);
    }

    async partial void ConnectToViewModel(VectorGraphicFlipViewItemModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;

        if (viewModel.Content is string svg)
        {
            await ShowSvgAsync(svg);
        }
    }

    partial void DisconnectFromViewModel(VectorGraphicFlipViewItemModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
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

    private void ScrollDummy_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ViewModel.IsContextMenuEnabeld)
        {
            dummy.ShowAttachedFlyout(args);
        }
    }
}
