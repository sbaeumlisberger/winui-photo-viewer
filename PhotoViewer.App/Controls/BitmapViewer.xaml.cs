using Essentials.NET;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using Essentials.NET.Logging;
using PhotoViewer.App.Views;
using System;
using Windows.Foundation;

namespace PhotoViewer.App.Controls;

public sealed partial class BitmapViewer : UserControl
{
    public event TypedEventHandler<BitmapViewer, ScrollViewerViewChangedEventArgs>? ViewChanged;

    public static readonly DependencyProperty BitmapImageProperty = DependencyPropertyHelper<BitmapViewer>.Register(nameof(BitmapImage), typeof(IBitmapImageModel));

    public static readonly DependencyProperty IsScaleUpEnabeldProperty = DependencyPropertyHelper<BitmapViewer>.Register(nameof(IsScaleUpEnabeld), typeof(bool), false);

    public static new readonly DependencyProperty ContentProperty = DependencyPropertyHelper<BitmapViewer>.Register(nameof(Content), typeof(object));

    public IBitmapImageModel? BitmapImage { get => (IBitmapImageModel?)GetValue(BitmapImageProperty); set => SetValue(BitmapImageProperty, value); }

    public bool IsScaleUpEnabeld { get => (bool)GetValue(IsScaleUpEnabeldProperty); set => SetValue(IsScaleUpEnabeldProperty, value); }

    public new object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    public ScrollViewer ScrollViewer => scrollViewer;

    private AnimatedBitmapRenderer? animatedBitmapRenderer;

    private readonly IColorProfileProvider colorProfileProvider;

    public BitmapViewer()
    {
        this.InitializeComponent();

        scrollViewer.EnableAdvancedZoomBehaviour();

        Unloaded += BitmapViewer_Unloaded;

        this.RegisterPropertyChangedCallbackSafely(IsEnabledProperty, OnIsEnabledChanged);
        this.RegisterPropertyChangedCallbackSafely(BitmapImageProperty, OnBitmapImageChanged);
        this.RegisterPropertyChangedCallbackSafely(IsScaleUpEnabeldProperty, OnIsScaleUpEnabeldChanged);

        colorProfileProvider = ColorProfileProvider.Instance;
        colorProfileProvider.ColorProfileChanged += ColorProfileProvider_ColorProfileChanged;
    }

    private void BitmapViewer_Unloaded(object sender, RoutedEventArgs e)
    {
        animatedBitmapRenderer.DisposeSafely(() => animatedBitmapRenderer = null);

        canvasControl.RemoveFromVisualTree();
        canvasControl = null;

        colorProfileProvider.ColorProfileChanged -= ColorProfileProvider_ColorProfileChanged;
    }

    private void ColorProfileProvider_ColorProfileChanged(object? sender, EventArgs e)
    {
        InvalidateCanvas();
    }

    private void OnBitmapImageChanged(DependencyObject sender, DependencyProperty dp)
    {
        animatedBitmapRenderer.DisposeSafely(() => animatedBitmapRenderer = null);

        scrollViewer.ChangeView(0, 0, 1);

        if (BitmapImage != null && BitmapImage.Frames.Count > 1)
        {
            animatedBitmapRenderer = new AnimatedBitmapRenderer(BitmapImage);
            animatedBitmapRenderer.FrameRendered += AnimatedBitmapRenderer_FrameRendered;
            animatedBitmapRenderer.IsPlaying = IsEnabled;
        }
        InvalidateCanvas();
    }

    private void OnIsScaleUpEnabeldChanged(DependencyObject sender, DependencyProperty dp)
    {
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
        InvalidateCanvas();
        ViewChanged?.Invoke(this, e);
    }

    private void InvalidateCanvas()
    {
        try
        {
            canvasControl.Invalidate();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to invalidate canvas", ex);
        }
    }

    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        try
        {
            if (BitmapImage is null)
            {
                args.DrawingSession.Clear(Colors.Transparent);
            }
            else
            {
                UpdateDummy(BitmapImage);
                DrawToCanvas(args.DrawingSession, BitmapImage);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to handle draw event", ex);
        }
    }

    private void UpdateDummy(IBitmapImageModel image)
    {
        double displayScale = canvasControl.Dpi / 96;
        var imageSize = new Size(image.SizeInDIPs.Width / displayScale, image.SizeInDIPs.Height / displayScale);

        double imageAspectRadio = imageSize.Width / imageSize.Height;
        double canvasAspectRadio = canvasControl.Size.Width / canvasControl.Size.Height;

        if (canvasAspectRadio > imageAspectRadio)
        {
            dummy.Height = IsScaleUpEnabeld ? canvasControl.Size.Height : Math.Min(canvasControl.Size.Height, imageSize.Height);
            dummy.Width = dummy.Height * imageAspectRadio;
        }
        else
        {
            dummy.Width = IsScaleUpEnabeld ? canvasControl.Size.Width : Math.Min(canvasControl.Size.Width, imageSize.Width);
            dummy.Height = dummy.Width / imageAspectRadio;
        }
    }

    private void DrawToCanvas(CanvasDrawingSession drawingSession, IBitmapImageModel image)
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

    private void DrawImageWithColorManagement(CanvasDrawingSession drawingSession, IBitmapImageModel image, Rect srcRect, Rect dstRect)
    {
        ColorManagementProfile? sourceColorProfile = null;

        if (image.ColorSpace.Profile is byte[] colorProfileBytes)
        {
            sourceColorProfile = ColorManagementProfile.CreateCustom(colorProfileBytes);
        }

        var outputColorProfile = colorProfileProvider.ColorProfile;

        ICanvasImage canvasImage = animatedBitmapRenderer != null ? animatedBitmapRenderer.RenderTarget : image.CanvasImage;

        drawingSession.Units = CanvasUnits.Pixels;
        double displayScale = canvasControl.Dpi / 96;
        var dstRectInPixels = new Rect(dstRect.X * displayScale, dstRect.Y * displayScale, dstRect.Width * displayScale, dstRect.Height * displayScale);

        if (sourceColorProfile is not null || outputColorProfile is not null)
        {
            using var colorProfileEffect = new ColorManagementEffect()
            {
                Source = canvasImage,
                SourceColorProfile = sourceColorProfile,
                OutputColorProfile = outputColorProfile
            };

            if (ColorManagementEffect.IsBestQualitySupported(drawingSession.Device))
            {
                colorProfileEffect.Quality = ColorManagementEffectQuality.Best;
            }

            drawingSession.DrawImage(colorProfileEffect, dstRectInPixels, srcRect, 1, CanvasImageInterpolation.NearestNeighbor);
        }
        else
        {
            drawingSession.DrawImage(canvasImage, dstRectInPixels, srcRect, 1, CanvasImageInterpolation.NearestNeighbor);
        }
    }


    private void Dummy_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (BitmapImage is null)
        {
            return;
        }

        double dummyWidthInDIPs = dummy.ActualWidth * dummy.XamlRoot.RasterizationScale;
        double orginalSizeZoomFactor = BitmapImage.SizeInDIPs.Width / dummyWidthInDIPs;

        if (MathUtils.ApproximateEquals(scrollViewer.ZoomFactor, 1))
        {
            scrollViewer.Zoom((float)orginalSizeZoomFactor);
        }
        else
        {
            scrollViewer.Zoom(1);
        }
    }

}
