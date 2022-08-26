using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using System;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using static System.Net.Mime.MediaTypeNames;

namespace PhotoViewerApp.Views;

public sealed partial class MediaFlipViewItem : UserControl
{
    public MediaFlipViewItemModel ViewModel => (MediaFlipViewItemModel)DataContext;

    private bool ScaleUp = false;

    public MediaFlipViewItem()
    {
        this.InitializeComponent();
        DataContextChanged += MediaFlipViewItem_DataContextChanged;
        ScrollViewerHelper.EnableAdvancedZoomBehaviour(scrollViewer);
        Unloaded += MediaFlipViewItem_Unloaded;
    }

    private void MediaFlipViewItem_Unloaded(object sender, RoutedEventArgs e)
    {
        canvasControl.RemoveFromVisualTree();
        canvasControl = null;
    }

    private void MediaFlipViewItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // this.Bindings.Update();
        scrollViewer.ChangeView(0, 0, 1);
        canvasControl.Invalidate();
    }

    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        canvasControl.Invalidate();
    }

    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (ViewModel.BitmapImage is null)
        {
            ViewModel.WaitUntilImageLoaded().ContinueWith(task =>
            {
                if (task.Result is IBitmapImage)
                {
                    canvasControl.Invalidate();
                }
            });
            return;
        }

        IBitmapImage image = ViewModel.BitmapImage;

        UpdateDummy(image);

        UpdateCanvas(args.DrawingSession, image);
    }

    private void UpdateDummy(IBitmapImage image)
    {
        double displayScale = XamlRoot.RasterizationScale;
        var imageSize = new Size(image.SizeInDIPs.Width / displayScale, image.SizeInDIPs.Height / displayScale);

        double imageAspectRadio = imageSize.Width / imageSize.Height;
        double canvasAspectRadio = canvasControl.Size.Width / canvasControl.Size.Height;

        if (canvasAspectRadio > imageAspectRadio)
        {
            dummy.Height = ScaleUp ? canvasControl.Size.Height : Math.Min(canvasControl.Size.Height, imageSize.Height);
            dummy.Width = dummy.Height * imageAspectRadio;
        }
        else
        {
            dummy.Width = ScaleUp ? canvasControl.Size.Width : Math.Min(canvasControl.Size.Width, imageSize.Width);
            dummy.Height = dummy.Width / imageAspectRadio;
        }
    }

    private void UpdateCanvas(CanvasDrawingSession drawingSession, IBitmapImage image)
    {
        double extentWidth = dummy.Width * scrollViewer.ZoomFactor;
        double extentHeight = dummy.Height * scrollViewer.ZoomFactor;

        double dstWidth = Math.Min(extentWidth, canvasControl.Size.Width);
        double dstHeight = Math.Min(extentHeight, canvasControl.Size.Height);
        double dstOffsetX = (canvasControl.Size.Width - dstWidth) / 2;
        double dstOffsetY = (canvasControl.Size.Height - dstHeight) / 2;
        Rect dstRect = new Rect(dstOffsetX, dstOffsetY, dstWidth, dstHeight);

        double scaleX = image.SizeInDIPs.Width / extentWidth;
        double scaleY = image.SizeInDIPs.Height / extentHeight;

        double srcX = scrollViewer.HorizontalOffset * scaleX;
        double srcY = scrollViewer.VerticalOffset * scaleY;
        double srcWidth = Math.Min(extentWidth, canvasControl.Size.Width) * scaleX;
        double srcHeight = Math.Min(extentHeight, canvasControl.Size.Height) * scaleY;

        var srcRect = new Rect(srcX, srcY, srcWidth, srcHeight);

        DrawImageWithColorManagement(drawingSession, image, srcRect, dstRect);
    }

    private void DrawImageWithColorManagement(CanvasDrawingSession drawingSession, IBitmapImage image, Rect srcRect, Rect dstRect)
    {
        ColorManagementProfile? sourceColorProfile = null;

        if (image.ColorSpace.Profile is byte[] colorProfileBytes)
        {
            sourceColorProfile = ColorManagementProfile.CreateCustom(colorProfileBytes);
        }

        var outputColorProfile = ColorProfileProvider.GetColorProfile();

        if (sourceColorProfile is not null || outputColorProfile is not null)
        {
            using var colorProfileEffect = new ColorManagementEffect()
            {
                Source = image.CanvasImage,
                SourceColorProfile = sourceColorProfile,
                OutputColorProfile = outputColorProfile
            };

            if (ColorManagementEffect.IsBestQualitySupported(drawingSession.Device))
            {
                colorProfileEffect.Quality = ColorManagementEffectQuality.Best;
            }

            drawingSession.DrawImage(colorProfileEffect, dstRect, srcRect);
        }
        else
        {
            drawingSession.DrawImage(image.CanvasImage, dstRect, srcRect);
        }
    }

}

