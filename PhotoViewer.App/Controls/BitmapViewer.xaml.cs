using Essentials.NET;
using Essentials.NET.Logging;
using Microsoft.Graphics.Canvas;
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

    private CanvasImageSource? canvas;

    public BitmapViewer()
    {
        this.InitializeComponent();

        scrollViewer.EnableAdvancedZoomBehaviour();

        Unloaded += BitmapViewer_Unloaded;
        Loaded += BitmapViewer_Loaded;
        SizeChanged += BitmapViewer_SizeChanged;

        this.RegisterPropertyChangedCallbackSafely(IsEnabledProperty, OnIsEnabledChanged);
        this.RegisterPropertyChangedCallbackSafely(BitmapImageProperty, OnBitmapImageChanged);
        this.RegisterPropertyChangedCallbackSafely(IsScaleUpEnabeldProperty, OnIsScaleUpEnabeldChanged);

        colorProfileProvider = ColorProfileProvider.Instance;
        colorProfileProvider.ColorProfileLoaded += ColorProfileProvider_ColorProfileLoaded;
    }

    private void BitmapViewer_Unloaded(object sender, RoutedEventArgs e)
    {
        animatedBitmapRenderer.DisposeSafely(() => animatedBitmapRenderer = null);

        imageControl.Source = null;
        canvas = null;

        colorProfileProvider.ColorProfileLoaded -= ColorProfileProvider_ColorProfileLoaded;
    }

    private void BitmapViewer_Loaded(object sender, RoutedEventArgs e)
    {
        Debug($"Loaded -> update scroll dummy and create canvas");
        UpdateScrollDummy();
        CreateCanvas();
    }

    private void BitmapViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded)
        {
            Debug("SizeChanged -> update scroll dummy and create new canvas");
            UpdateScrollDummy();
            CreateCanvas();
        }
    }

    private void CreateCanvas()
    {
        if (ActualWidth == 0 || ActualHeight == 0)
        {
            Debug("Do not create canvas because width or heigt is zero");
            return;
        }

        uint canvasWidthInPixels = (uint)Math.Floor(ActualWidth * XamlRoot.RasterizationScale);
        uint canvasHeightInPixels = (uint)Math.Floor(ActualHeight * XamlRoot.RasterizationScale);

        canvas = new CanvasImageSource(CanvasDevice.GetSharedDevice(), canvasWidthInPixels, canvasHeightInPixels, 96);

        imageControl.Source = canvas;

        InvalidateCanvas();
    }

    private void ColorProfileProvider_ColorProfileLoaded(object? sender, EventArgs e)
    {
        Debug($"ColorProfileLoaded -> invalidate canvas");
        InvalidateCanvas();
    }

    private void OnBitmapImageChanged(DependencyObject sender, DependencyProperty dp)
    {
        Debug($"OnBitmapImageChanged ({BitmapImage}) -> update scroll dummy and invalidate canvas");

        animatedBitmapRenderer.DisposeSafely(() => animatedBitmapRenderer = null);

        if (scrollViewer.ZoomFactor != 1)
        {
            scrollViewer.ChangeView(0, 0, 1, true);
        }

        UpdateScrollDummy();

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
        Debug($"OnIsScaleUpEnabeldChanged -> update scroll dummy and invalidate canvas");

        UpdateScrollDummy();
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
        try
        {
            if (canvas is null)
            {
                Debug("Canvas is not initialized yet, skip drawing");
                return;
            }

            if (double.IsNaN(scrollDummy.Width) || double.IsNaN(scrollDummy.Height))
            {
                Debug("Scroll dummy is not initialized yet, skip drawing");
                return;
            }

            if (!colorProfileProvider.IsInitialized)
            {
                Debug("Color profile provider is not initialized yet, skip drawing");
                return;
            }

            using var drawingSession = canvas.CreateDrawingSession(Colors.Transparent);

            if (BitmapImage is null)
            {
                Debug("BitmapImage is not set, nothing to draw");
                return;
            }

            DrawImageToCanvas(canvas, drawingSession, BitmapImage);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to draw", ex);
        }
    }

    private void UpdateScrollDummy()
    {
        if (XamlRoot is null)
        {
            Debug("Skip updating scroll dummy because XamlRoot is null");
            return;
        }

        if (ActualWidth == 0 || ActualHeight == 0)
        {
            Debug("Skip updating scroll dummy because width or height is zero");
            return;
        }

        if (BitmapImage is null)
        {
            Debug("Skip updating scroll dummy because BitmapImage is null");
            return;
        }

        double displayScale = XamlRoot.RasterizationScale;

        double imageWidthInDIPs = BitmapImage.SizeInPixels.Width / displayScale;
        double imageHeightInDIPs = BitmapImage.SizeInPixels.Height / displayScale;

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

    private void DrawImageToCanvas(CanvasImageSource canvas, CanvasDrawingSession drawingSession, IBitmapImageModel image)
    {
        double displayScale = XamlRoot.RasterizationScale;

        double extentWidthInPixels = Math.Round(scrollDummy.Width * displayScale * scrollViewer.ZoomFactor);
        double extentHeightInPixels = Math.Round(scrollDummy.Height * displayScale * scrollViewer.ZoomFactor);

        double dstWidth = Math.Min(extentWidthInPixels, canvas.SizeInPixels.Width);
        double dstHeight = Math.Min(extentHeightInPixels, canvas.SizeInPixels.Height);
        double dstX = Math.Round((canvas.SizeInPixels.Width - dstWidth) / 2d);
        double dstY = Math.Round((canvas.SizeInPixels.Height - dstHeight) / 2d);
        Rect dstRectInPixels = new Rect(dstX, dstY, dstWidth, dstHeight);

        double srcWidth = Math.Round(image.SizeInPixels.Width * (dstWidth / extentWidthInPixels));
        double srcHeight = Math.Round(image.SizeInPixels.Height * (dstHeight / extentHeightInPixels));
        double srcX = Math.Round(image.SizeInPixels.Width * (scrollViewer.HorizontalOffset / Math.Max(1, scrollViewer.ExtentWidth)));
        double srcY = Math.Round(image.SizeInPixels.Height * (scrollViewer.VerticalOffset / Math.Max(1, scrollViewer.ExtentHeight)));
        srcX = Math.Min(srcX, image.SizeInPixels.Width - srcWidth);
        srcY = Math.Min(srcY, image.SizeInPixels.Height - srcHeight);
        Rect srcRectInPixels = new Rect(srcX, srcY, srcWidth, srcHeight);

        DrawImageWithColorManagement(drawingSession, image, dstRectInPixels, srcRectInPixels);
    }

    private void DrawImageWithColorManagement(CanvasDrawingSession drawingSession, IBitmapImageModel image, Rect dstRect, Rect srcRect)
    {
        ColorManagementProfile? sourceColorProfile = null;

        if (image.ColorSpace.Profile is byte[] colorProfileBytes)
        {
            sourceColorProfile = ColorManagementProfile.CreateCustom(colorProfileBytes);
        }

        var outputColorProfile = colorProfileProvider.ColorProfile;

        ICanvasImage canvasImage = animatedBitmapRenderer != null ? animatedBitmapRenderer.RenderTarget : image.CanvasImage;

        var interpolationMode = CanvasImageInterpolation.NearestNeighbor;

        if (dstRect.Width < srcRect.Width || dstRect.Height < srcRect.Height)
        {
            interpolationMode = CanvasImageInterpolation.HighQualityCubic;
        }

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

            drawingSession.DrawImage(colorProfileEffect, dstRect, srcRect, 1, interpolationMode);
        }
        else
        {
            drawingSession.DrawImage(canvasImage, dstRect, srcRect, 1, interpolationMode);
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

}
