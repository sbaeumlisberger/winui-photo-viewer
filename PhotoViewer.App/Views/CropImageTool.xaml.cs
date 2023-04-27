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
    public CropImageToolModel? ViewModel => DataContext as CropImageToolModel;

    public CropImageTool()
    {
        this.InitializeComponentMVVM(/*updateBindingsAlways: true*/);
    }

    partial void ConnectToViewModel(CropImageToolModel viewModel)
    {
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateSelectionRect();
    }

    partial void DisconnectFromViewModel(CropImageToolModel viewModel)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectionInPixels))
        {
            UpdateSelectionRect();
        }
    }

    private void UpdateSelectionRect()
    {
        if (ViewModel is null)
        {
            return;
        }

        var selection = ViewModel.SelectionInPixels;
        var imageSize = ViewModel.ImageSizeInPixels;
        Canvas.SetLeft(selectionRect, selection.X / imageSize.Width * ActualWidth);
        Canvas.SetTop(selectionRect, selection.Y / imageSize.Height * ActualHeight);
        selectionRect.Width = selection.Width / imageSize.Width * ActualWidth;
        selectionRect.Height = selection.Height / imageSize.Height * ActualHeight;

        UpdateToolbar(selectionRect.GetBounds());
    }

    private void UpdateToolbar(Rect selection)
    {
        Canvas.SetLeft(toolbar, selection.Left + selection.Width / 2 - toolbar.ActualWidth / 2);
        Canvas.SetTop(toolbar, Math.Min(selection.Bottom + 8, selectionCanvas.ActualHeight - toolbar.ActualHeight));
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
        var selection = selectionRect.GetBounds();

        UpdateToolbar(selectionRect.GetBounds());

        var imageSizeInPixels = ViewModel!.ImageSizeInPixels;

        var selectionInPixel = new Rect(
            selection.X / selectionCanvas.ActualWidth * imageSizeInPixels.Width,
            selection.Y / selectionCanvas.ActualHeight * imageSizeInPixels.Height,
            selection.Width / selectionCanvas.ActualWidth * imageSizeInPixels.Width,
            selection.Height / selectionCanvas.ActualHeight * imageSizeInPixels.Height);

        ViewModel.SelectionInPixels = selectionInPixel;
    }
}
