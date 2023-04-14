using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Views;
using System;
using Windows.Foundation;

namespace PhotoViewer.App.Controls;

public sealed partial class BitmapViewer : UserControl
{
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

        ScrollViewerHelper.EnableAdvancedZoomBehaviour(scrollViewer);

        Unloaded += BitmapViewer_Unloaded;

        this.RegisterPropertyChangedCallbackSafely(IsEnabledProperty, OnIsEnabledChanged);
        this.RegisterPropertyChangedCallbackSafely(BitmapImageProperty, OnBitmapImageChanged);
        this.RegisterPropertyChangedCallbackSafely(IsScaleUpEnabeldProperty, OnIsScaleUpEnabeldChanged);

        colorProfileProvider = ColorProfileProvider.Instance;
        colorProfileProvider.ColorProfileChanged += ColorProfileProvider_ColorProfileChanged;
    }

    private void BitmapViewer_Unloaded(object sender, RoutedEventArgs e)
    {
        DisposeUtil.DisposeSafely(ref animatedBitmapRenderer);

        canvasControl.RemoveFromVisualTree();
        canvasControl = null;

        colorProfileProvider.ColorProfileChanged -= ColorProfileProvider_ColorProfileChanged;
    }

    private void ColorProfileProvider_ColorProfileChanged(object? sender, EventArgs e)
    {
        canvasControl.Invalidate();
    }

    private void OnBitmapImageChanged(DependencyObject sender, DependencyProperty dp)
    {
        DisposeUtil.DisposeSafely(ref animatedBitmapRenderer);

        scrollViewer.ChangeView(0, 0, 1);

        if (BitmapImage != null && BitmapImage.Frames.Count > 1)
        {
            animatedBitmapRenderer = new AnimatedBitmapRenderer(BitmapImage);
            animatedBitmapRenderer.FrameRendered += AnimatedBitmapRenderer_FrameRendered;
            animatedBitmapRenderer.IsPlaying = IsEnabled;
        }
        canvasControl.Invalidate();
    }

    private void OnIsScaleUpEnabeldChanged(DependencyObject sender, DependencyProperty dp)
    {
        canvasControl.Invalidate();
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
        canvasControl.Invalidate();
    }

    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        canvasControl.Invalidate();
    }

    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
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

    private void UpdateDummy(IBitmapImageModel image)
    {
        double displayScale = XamlRoot.RasterizationScale;
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

            drawingSession.DrawImage(colorProfileEffect, dstRect, srcRect);
        }
        else
        {
            drawingSession.DrawImage(canvasImage, dstRect, srcRect);
        }
    }
}
