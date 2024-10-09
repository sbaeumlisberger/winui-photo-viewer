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
using Windows.Foundation;

namespace PhotoViewer.App.Controls;

public sealed partial class BitmapViewer : UserControl
{
    public event TypedEventHandler<BitmapViewer, ScrollViewerViewChangedEventArgs>? ViewChanged;

    public IBitmapImageModel? BitmapImage { get => bitmapImage; set => SetField(ref bitmapImage, value, OnBitmapImageChanged); }
    private IBitmapImageModel? bitmapImage = null;

    public bool IsScaleUpEnabeld { get => isScaleUpEnabeld; set => SetField(ref isScaleUpEnabeld, value, OnIsScaleUpEnabeldChanged); }
    private bool isScaleUpEnabeld = false;

    public new object? Content { get => contentPresenter.Content; set { contentPresenter.Content = value; } }

    public ScrollViewer ScrollViewer => scrollViewer;

    private AnimatedBitmapRenderer? animatedBitmapRenderer;

    private readonly IColorProfileProvider colorProfileProvider;

    public BitmapViewer()
    {
        this.InitializeComponent();

        scrollViewer.EnableAdvancedZoomBehaviour();

        Unloaded += BitmapViewer_Unloaded;

        this.RegisterPropertyChangedCallbackSafely(IsEnabledProperty, OnIsEnabledChanged);

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
        if (BitmapImage is not null)
        {
            Log.Debug("ColorProfileProvider_ColorProfileLoaded->InvalidateCanvas");
            InvalidateCanvas();
        }
    }

    private void OnBitmapImageChanged()
    {
        animatedBitmapRenderer.DisposeSafely(() => animatedBitmapRenderer = null);

        if (scrollViewer.ZoomFactor != 1)
        {
            scrollViewer.ChangeView(0, 0, 1);
        }

        if (BitmapImage != null && BitmapImage.Frames.Count > 1)
        {
            animatedBitmapRenderer = new AnimatedBitmapRenderer(BitmapImage);
            animatedBitmapRenderer.FrameRendered += AnimatedBitmapRenderer_FrameRendered;
            animatedBitmapRenderer.IsPlaying = IsEnabled;
        }

        Log.Debug("OnBitmapImageChanged->InvalidateCanvas");
        InvalidateCanvas();
    }

    private void OnIsScaleUpEnabeldChanged()
    {
        if (BitmapImage is not null)
        {
            Log.Debug("OnIsScaleUpEnabeldChanged->InvalidateCanvas");
            InvalidateCanvas();
        }
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
        Log.Debug("ScrollViewer_ViewChanged->InvalidateCanvas");
        InvalidateCanvas();
        ViewChanged?.Invoke(this, e);
    }

    private void InvalidateCanvas()
    {
        try
        {
            //Log.Debug($"InvalidateCanvas {BitmapImage?.ID}");
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
            if (!colorProfileProvider.IsInitialized)
            {
                Log.Debug("Color profile provider is not initialized yet, skip drawing");
                return;
            }
            else if (BitmapImage is null)
            {
                Log.Debug("Image not set, skip drawing");
                args.DrawingSession.Clear(Colors.Transparent);
            }
            else
            {
                //Log.Debug($"CanvasControl_Draw called for {BitmapImage.ID}");
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

        var interpolationMode = CanvasImageInterpolation.NearestNeighbor;

        if (dstRectInPixels.Width < srcRect.Width || dstRectInPixels.Height < srcRect.Height)
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

            drawingSession.DrawImage(colorProfileEffect, dstRectInPixels, srcRect, 1, interpolationMode);
        }
        else
        {
            drawingSession.DrawImage(canvasImage, dstRectInPixels, srcRect, 1, interpolationMode);
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

    private static void SetField<T>(ref T field, T value, Action onChanged)
    {
        bool changed = !Equals(value, field);
        field = value;
        if (changed)
        {
            onChanged();
        }
    }
}
