using Essentials.NET.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using System;
using System.Diagnostics;
using System.Numerics;

namespace PhotoViewer.App.Controls;

public sealed partial class CanvasImageControl : UserControl
{
    public static readonly DependencyProperty CanvasImageProperty = DependencyPropertyHelper<CanvasImageControl>
        .Register<ICanvasImage?>(nameof(CanvasImage), null, (s, e) => s.CanvasImageProperty_Changed());

    public ICanvasImage? CanvasImage { get => (ICanvasImage?)GetValue(CanvasImageProperty); set => SetValue(CanvasImageProperty, value); }

    private double scaleFactor = 1;

    private CanvasControl? canvasControl;

    public CanvasImageControl()
    {
        this.InitializeComponent();
    }

    private void CanvasImageControl_Loaded(object sender, RoutedEventArgs e)
    {
        canvasControl = new CanvasControl();
        canvasControl.Draw += CanvasControl_Draw;
        canvasControl.CreateResources += CanvasControl_CreateResources;
        Content = canvasControl;

        if (CanvasImage is not null)
        {
            CanvasImageProperty_Changed();
        }
    }

    private void CanvasControl_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        if (args.Reason == CanvasCreateResourcesReason.NewDevice)
        {
            Debug.WriteLine("New device");
        }
    }

    private void CanvasImageControl_Unloaded(object sender, RoutedEventArgs e)
    {
        canvasControl?.RemoveFromVisualTree();
        canvasControl = null;
        Content = null;
    }

    private void CanvasImageProperty_Changed()
    {
        if (canvasControl is null)
        {
            return;
        }

        canvasControl.Width = double.NaN;
        canvasControl.Height = double.NaN;

        if (CanvasImage is not null)
        {
            try
            {
                var imageSize = CanvasImage.GetBounds(CanvasDevice.GetSharedDevice());

                if (double.IsNaN(Width) && double.IsNaN(Height))
                {
                    double scaleX = MaxWidth != double.PositiveInfinity ? MaxWidth / imageSize.Width : 1;
                    double scaleY = MaxHeight != double.PositiveInfinity ? MaxHeight / imageSize.Height : 1;
                    scaleFactor = Math.Min(scaleX, scaleY);
                    canvasControl.Width = imageSize.Width * scaleFactor;
                    canvasControl.Height = imageSize.Height * scaleFactor;
                }
                else
                {
                    double scaleX = Width / imageSize.Width;
                    double scaleY = Height / imageSize.Height;
                    scaleFactor = Math.Min(scaleX, scaleY);
                    canvasControl.Width = Width;
                    canvasControl.Height = Height;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to process canvas image", ex);
            }
        }

        canvasControl.Invalidate();
    }

    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (CanvasImage is null)
        {
            args.DrawingSession.Clear(Colors.Transparent);
            return;
        }
        try
        {
            var scaledImage = new ScaleEffect()
            {
                Source = CanvasImage,
                Scale = new Vector2((float)scaleFactor),
            };

            args.DrawingSession.DrawImage(scaledImage);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to draw canvas image", ex);
        }
    }

}
