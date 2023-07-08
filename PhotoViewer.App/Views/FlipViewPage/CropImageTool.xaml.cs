using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Controls;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.ComponentModel;
using Windows.Foundation;

namespace PhotoViewer.App.Views;

public sealed partial class CropImageTool : UserControl, IMVVMControl<CropImageToolModel>
{
    public CropImageTool()
    {
        this.InitializeComponentMVVM();
    }

    partial void ConnectToViewModel(CropImageToolModel viewModel)
    {
        viewModel.Subscribe(this, nameof(viewModel.SelectionInPixels), UpdateSelectionRect);
        UpdateSelectionRect();
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

        if (ViewModel is null || ViewModel.SelectionInPixels.IsEmpty)
        {
            return;
        }
   
        var selectionInPixels = ViewModel.SelectionInPixels;
        var imageSizeInPixels = ViewModel.ImageSizeInPixels;

        var selectionRectBounds = new Rect(
            selectionInPixels.X / imageSizeInPixels.Width * ActualWidth,
            selectionInPixels.Y / imageSizeInPixels.Height * ActualHeight,
            selectionInPixels.Width / imageSizeInPixels.Width * ActualWidth,
            selectionInPixels.Height / imageSizeInPixels.Height * ActualHeight);

        selectionRect.SetBounds(selectionRectBounds);

        UpdateToolbar(selectionRectBounds);
    }

    private void UpdateToolbar(Rect selectionRectBounds)
    {
        Canvas.SetLeft(toolbar, selectionRectBounds.Left + selectionRectBounds.Width / 2 - toolbar.ActualWidth / 2);
        Canvas.SetTop(toolbar, Math.Min(selectionRectBounds.Bottom + 8, selectionCanvas.ActualHeight - toolbar.ActualHeight));
        toolbar.Opacity = 1;
        toolbar.IsHitTestVisible = true;
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
        UpdateToolbar(selectionRect.GetBounds());

        var selectionRectBounds = selectionRect.GetBounds();
        var imageSizeInPixels = ViewModel!.ImageSizeInPixels;

        var selectionInPixels = new Rect(
            selectionRectBounds.X / ActualWidth * imageSizeInPixels.Width,
            selectionRectBounds.Y / ActualHeight * imageSizeInPixels.Height,
            selectionRectBounds.Width / ActualWidth * imageSizeInPixels.Width,
            selectionRectBounds.Height / ActualHeight * imageSizeInPixels.Height);

        ViewModel.SelectionInPixels = selectionInPixels;
    }
}
