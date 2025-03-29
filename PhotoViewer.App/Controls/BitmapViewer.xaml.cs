using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Views;
using PhotoViewer.Core.Models;
using System;
using System.Diagnostics;
using Windows.Foundation;

namespace PhotoViewer.App.Controls;

public sealed partial class BitmapViewer : UserControl
{
    public event TypedEventHandler<BitmapViewer, ScrollViewerViewChangedEventArgs>? ViewChanged;

    public static readonly DependencyProperty BitmapImageProperty = DependencyPropertyHelper<BitmapViewer>.Register<IBitmapImageModel?>(nameof(BitmapImage), null);

    public static readonly DependencyProperty IsScaleUpEnabeldProperty = DependencyPropertyHelper<BitmapViewer>.Register(nameof(IsScaleUpEnabeld), false);

    public IBitmapImageModel? BitmapImage { get => (IBitmapImageModel?)GetValue(BitmapImageProperty); set => SetValue(BitmapImageProperty, value); }

    public bool IsScaleUpEnabeld { get => (bool)GetValue(IsScaleUpEnabeldProperty); set => SetValue(IsScaleUpEnabeldProperty, value); }

    public new object Content { get => contentPresenter.Content; set => contentPresenter.Content = value; }

    public ScrollViewer ScrollViewer => scrollViewer;

    private AnimatedBitmapRenderer? animatedBitmapRenderer;

    private readonly IColorProfileProvider colorProfileProvider;

#if DEBUG
    private readonly Guid debugId = Guid.NewGuid();
#endif

    public BitmapViewer()
    {
        this.InitializeComponent();

        scrollViewer.EnableAdvancedZoomBehaviour();

        Unloaded += BitmapViewer_Unloaded;

        this.RegisterPropertyChangedCallbackSafely(IsEnabledProperty, OnIsEnabledChanged);
        this.RegisterPropertyChangedCallbackSafely(BitmapImageProperty, OnBitmapImageChanged);
        this.RegisterPropertyChangedCallbackSafely(IsScaleUpEnabeldProperty, OnIsScaleUpEnabeldChanged);

        colorProfileProvider = ColorProfileProvider.Instance;
        colorProfileProvider.ColorProfileLoaded += ColorProfileProvider_ColorProfileLoaded;
    }

    private void BitmapViewer_Unloaded(object sender, RoutedEventArgs e)
    {
        animatedBitmapRenderer.DisposeSafely(() => animatedBitmapRenderer = null);

        canvasControl.RemoveFromVisualTree();
        canvasControl = null;

        colorProfileProvider.ColorProfileLoaded -= ColorProfileProvider_ColorProfileLoaded;
    }

    private void ColorProfileProvider_ColorProfileLoaded(object? sender, EventArgs e)
    {
        Debug("ColorProfileLoaded -> invalidate canvas");
        InvalidateCanvas();
    }

    private void OnBitmapImageChanged(DependencyObject sender, DependencyProperty dp)
    {
        Debug($"OnBitmapImageChanged ({BitmapImage}) -> invalidate canvas");

        animatedBitmapRenderer.DisposeSafely(() => animatedBitmapRenderer = null);

        if (scrollViewer.ZoomFactor != 1)
        {
            scrollViewer.ChangeView(0, 0, 1, true);
        }

        if (BitmapImage != null && BitmapImage.Frames.Count > 1)
        {
            animatedBitmapRenderer = new AnimatedBitmapRenderer(BitmapImage);
            animatedBitmapRenderer.FrameRendered += AnimatedBitmapRenderer_FrameRendered;
            animatedBitmapRenderer.IsPlaying = IsEnabled;
        }
        else
        {
            InvalidateCanvas();
        }
    }

    private void OnIsScaleUpEnabeldChanged(DependencyObject sender, DependencyProperty dp)
    {
        Debug("OnIsScaleUpEnabeldChanged -> invalidate canvas");
        InvalidateCanvas();
    }

    private void OnIsEnabledChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (animatedBitmapRenderer != null)
        {
            animatedBitmapRenderer.IsPlaying = IsEnabled;
        }
    }

    private void AnimatedBitmapRenderer_FrameRendered(AnimatedBitmapRenderer sender, EventArgs args)
    {
        InvalidateCanvas();
    }

    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        Debug($"ScrollViewer_ViewChanged -> invalidate canvas");
        InvalidateCanvas();
        ViewChanged?.Invoke(this, e);
    }

    private void InvalidateCanvas()
    {
        canvasControl.Invalidate();
    }

    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            if (!colorProfileProvider.IsInitialized)
            {
                Debug("Color profile provider is not initialized yet, skip drawing");
                return;
            }

            if (BitmapImage is null)
            {
                Debug("BitmapImage is not set, nothing to draw");
                return;
            }

            UpdateScrollDummy(BitmapImage);

            using var drawingSession = args.DrawingSession;

            DrawImageToCanvas(drawingSession, BitmapImage);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to draw", ex);
        }
    }

    private void UpdateScrollDummy(IBitmapImageModel bitmapImage)
    {
        double imageWidthInDIPs = PixelsToDips(bitmapImage.SizeInPixels.Width);
        double imageHeightInDIPs = PixelsToDips(bitmapImage.SizeInPixels.Height);

        double imageAspectRadio = imageWidthInDIPs / imageHeightInDIPs;
        double canvasAspectRadio = ActualWidth / ActualHeight;

        if (canvasAspectRadio > imageAspectRadio)
        {
            scrollDummy.Height = IsScaleUpEnabeld ? ActualHeight : Math.Min(ActualHeight, imageHeightInDIPs);
            scrollDummy.Width = scrollDummy.Height * imageAspectRadio;
        }
        else
        {
            scrollDummy.Width = IsScaleUpEnabeld ? ActualWidth : Math.Min(ActualWidth, imageWidthInDIPs);
            scrollDummy.Height = scrollDummy.Width / imageAspectRadio;
        }
    }

    private void DrawImageToCanvas(CanvasDrawingSession drawingSession, IBitmapImageModel image)
    {
        double extendWidthInDIPs = scrollDummy.Width * scrollViewer.ZoomFactor;
        double extendHeightInDIPs = scrollDummy.Height * scrollViewer.ZoomFactor;

        double extentWidthInPixels = DipsToPixels(extendWidthInDIPs);
        double extentHeightInPixels = DipsToPixels(extendHeightInDIPs);

        double canvasWidthInPixels = DipsToPixels(canvasControl.ActualWidth);
        double canvasHeightInPixels = DipsToPixels(canvasControl.ActualHeight);

        double dstWidth = Math.Min(extentWidthInPixels, canvasWidthInPixels);
        double dstHeight = Math.Min(extentHeightInPixels, canvasHeightInPixels);
        double dstX = (canvasWidthInPixels - dstWidth) / 2d;
        double dstY = (canvasHeightInPixels - dstHeight) / 2d;
        Rect dstRectInPixels = new Rect(dstX, dstY, dstWidth, dstHeight);

        double srcWidth = Math.Round(image.SizeInPixels.Width * (dstWidth / extentWidthInPixels));
        double srcHeight = Math.Round(image.SizeInPixels.Height * (dstHeight / extentHeightInPixels));
        double srcX = Math.Round(image.SizeInPixels.Width * (scrollViewer.HorizontalOffset / extendWidthInDIPs));
        double srcY = Math.Round(image.SizeInPixels.Height * (scrollViewer.VerticalOffset / extendHeightInDIPs));
        srcX = Math.Min(srcX, image.SizeInPixels.Width - srcWidth);
        srcY = Math.Min(srcY, image.SizeInPixels.Height - srcHeight);
        Rect srcRectInPixels = new Rect(srcX, srcY, srcWidth, srcHeight);

        DrawImageWithColorManagement(drawingSession, image, dstRectInPixels, srcRectInPixels);
    }

    private void DrawImageWithColorManagement(CanvasDrawingSession drawingSession, IBitmapImageModel image, Rect dstRectInPixels, Rect srcRectInPixels)
    {
        drawingSession.Units = CanvasUnits.Pixels;
        drawingSession.Antialiasing = CanvasAntialiasing.Aliased;

        ICanvasImage canvasImage = animatedBitmapRenderer != null ? animatedBitmapRenderer.RenderTarget : image.CanvasImage;

        var interpolationMode = CanvasImageInterpolation.NearestNeighbor;

        if (dstRectInPixels.Width < srcRectInPixels.Width || dstRectInPixels.Height < srcRectInPixels.Height)
        {
            interpolationMode = CanvasImageInterpolation.HighQualityCubic;
        }

        if (colorProfileProvider.ColorProfile is not null)
        {
            using var colorProfileEffect = new ColorManagementEffect()
            {
                Source = canvasImage,
                OutputColorProfile = colorProfileProvider.ColorProfile
            };

            if (ColorManagementEffect.IsBestQualitySupported(drawingSession.Device))
            {
                colorProfileEffect.Quality = ColorManagementEffectQuality.Best;
            }

            drawingSession.DrawImage(colorProfileEffect, dstRectInPixels, srcRectInPixels, 1, interpolationMode);
        }
        else
        {
            drawingSession.DrawImage(canvasImage, dstRectInPixels, srcRectInPixels, 1, interpolationMode);
        }

        //Log.Debug("Image " + image.ID + " drawn");
        Program.NotifyImageDrawn();
    }

    private void Dummy_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (BitmapImage is null)
        {
            return;
        }

        double scrollDummyWidthInPixels = scrollDummy.ActualWidth * scrollDummy.XamlRoot.RasterizationScale;
        double orginalSizeZoomFactor = BitmapImage.SizeInPixels.Width / scrollDummyWidthInPixels;

        if (MathUtils.ApproximateEquals(scrollViewer.ZoomFactor, 1))
        {
            scrollViewer.Zoom((float)orginalSizeZoomFactor);
        }
        else
        {
            scrollViewer.Zoom(1);
        }
    }

    [Conditional("DEBUG")]
    private void Debug(string message)
    {
#if DEBUG
        //Log.Debug($"BitmapViewer({debugId}): {message}");
#endif
    }

    private double PixelsToDips(uint pixels)
    {
        return pixels / XamlRoot.RasterizationScale;
    }

    private int DipsToPixels(double dips)
    {
        return (int)Math.Round(dips * XamlRoot.RasterizationScale);
    }
}
