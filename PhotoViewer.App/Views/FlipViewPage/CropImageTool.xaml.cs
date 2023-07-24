using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Graphics;

namespace PhotoViewer.App.Views;

public sealed partial class CropImageTool : UserControl, IMVVMControl<CropImageToolModel>
{
    public CropImageTool()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(CropImageToolModel viewModel)
    {
        viewModel.Subscribe(this, nameof(viewModel.SelectionInPixels), UpdateSelectionRect, initialCallback: true);
        viewModel.Subscribe(this, nameof(viewModel.UIScaleFactor), ViewModel_UIScaleFactorChanged);
    }

    partial void DisconnectFromViewModel(CropImageToolModel viewModel)
    {
        viewModel.UnsubscribeAll(this);
    }

    private void UpdateSelectionRect()
    {
        if (ActualWidth == 0 || ActualHeight == 0)
        {
            return;
        }

        if (ViewModel is null || ViewModel.SelectionInPixels == default)
        {
            return;
        }

        var selectionInPixels = ViewModel.SelectionInPixels;
        var imageSizeInPixels = ViewModel.ImageSizeInPixels;

        var selectionRectBounds = CalculateSelectionRectBounds(selectionInPixels, imageSizeInPixels);

        selectionRect.SetBounds(selectionRectBounds);

        UpdateSelectionInfo(selectionRectBounds);
        UpdateToolbar(selectionRectBounds);
    }

    private void ViewModel_UIScaleFactorChanged()
    {
        var selectionRectBounds = selectionRect.GetBounds();
        UpdateSelectionInfo(selectionRectBounds);
        UpdateToolbar(selectionRectBounds);
    }

    private void UpdateToolbar(Rect selectionRectBounds)
    {
        double toolbarWidth = toolbar.ActualWidth * ViewModel!.UIScaleFactor;
        double toolbarHeight = toolbar.ActualHeight * ViewModel!.UIScaleFactor;
        double left = selectionRectBounds.GetCenterX() - toolbarWidth / 2;
        Canvas.SetLeft(toolbar, Math.Clamp(left, 0, selectionCanvas.ActualWidth - toolbarWidth));
        Canvas.SetTop(toolbar, Math.Min(selectionRectBounds.Bottom, selectionCanvas.ActualHeight - toolbarHeight));
    }

    private void UpdateSelectionInfo(Rect selectionRectBounds)
    {
        double selectionInfoWidth = selectionInfo.ActualWidth * ViewModel!.UIScaleFactor;
        double selectionInfoHeight = selectionInfo.ActualHeight * ViewModel!.UIScaleFactor;
        double left = selectionRectBounds.GetCenterX() - selectionInfoWidth / 2;
        Canvas.SetLeft(selectionInfo, Math.Clamp(left, 0, selectionCanvas.ActualWidth - selectionInfoWidth));
        Canvas.SetTop(selectionInfo, Math.Max(selectionRectBounds.Top - selectionInfoHeight, 0));
    }

    private void SelectionCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSelectionRect();
    }

    private void SelectionRect_InteractionStarted(SelectionRect sender, EventArgs args)
    {
        toolbar.Opacity = 0;
        toolbar.IsHitTestVisible = false;
    }

    private void SelectionRect_InteractionEnded(SelectionRect sender, EventArgs args)
    {
        toolbar.Opacity = 1;
        toolbar.IsHitTestVisible = true;
    }

    private void SelectionRect_BoundsChanging(SelectionRect sender, SelectionRect.BoundsChangingEventArgs args)
    {
        var imageSizeInPixels = ViewModel!.ImageSizeInPixels;

        var selectionInPixels = new RectInt32(
            (int)Math.Round(args.NewBounds.X / ActualWidth * imageSizeInPixels.Width),
            (int)Math.Round(args.NewBounds.Y / ActualHeight * imageSizeInPixels.Height),
            (int)Math.Max(1, Math.Round(args.NewBounds.Width / ActualWidth * imageSizeInPixels.Width)),
            (int)Math.Max(1, Math.Round(args.NewBounds.Height / ActualHeight * imageSizeInPixels.Height)));

        ViewModel.SelectionInPixels = selectionInPixels;

        var adjustedSelectionRectBounds = CalculateSelectionRectBounds(selectionInPixels, imageSizeInPixels);

        args.NewBounds = adjustedSelectionRectBounds;
    }

    private Rect CalculateSelectionRectBounds(RectInt32 selectionInPixels, SizeInt32 imageSizeInPixels)
    {
        return new Rect(
            (double)selectionInPixels.X / imageSizeInPixels.Width * selectionCanvas.ActualWidth,
            (double)selectionInPixels.Y / imageSizeInPixels.Height * selectionCanvas.ActualHeight,
            (double)selectionInPixels.Width / imageSizeInPixels.Width * selectionCanvas.ActualWidth,
            (double)selectionInPixels.Height / imageSizeInPixels.Height * selectionCanvas.ActualHeight);
    }
}
