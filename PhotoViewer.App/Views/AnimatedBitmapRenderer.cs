using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using PhotoViewer.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PhotoViewer.App.Views;

public class AnimatedBitmapRenderer : IDisposable
{
    public event TypedEventHandler<AnimatedBitmapRenderer, EventArgs>? FrameRendered;

    public CanvasRenderTarget RenderTarget { get; }

    public bool IsPlaying
    {
        get => isPlaying;
        set
        {
            if (value != isPlaying)
            {
                isPlaying = value;
                if (isPlaying)
                {
                    currentFrameIndex = 0;
                    _ = RenderAsync();
                }
            }
        }
    }

    private bool isPlaying = false;

    private readonly IReadOnlyList<IBitmapFrame> frames;

    private readonly CanvasDrawingSession drawingSession;

    private int currentFrameIndex = 0;

    private bool isDisposed;

    public AnimatedBitmapRenderer(IBitmapImage animatedBitmap)
    {
        frames = animatedBitmap.Frames;

        var device = animatedBitmap.Device;
        var size = animatedBitmap.SizeInPixels;

        RenderTarget = new CanvasRenderTarget(device, size.Width, size.Height, 96);
        drawingSession = RenderTarget.CreateDrawingSession();
        drawingSession.DrawImage(frames[0].CanvasImage);
        drawingSession.Flush();
    }

    public void Dispose()
    {
        isDisposed = true;
        FrameRendered = null;
        drawingSession.Dispose();
        RenderTarget.Dispose();
    }

    private async Task RenderAsync()
    {
        if (frames.Count > 1)
        {
            var stopwatch = new Stopwatch();
            while (IsPlaying && !isDisposed)
            {
                stopwatch.Restart();
                double delay = RenderNextFrame();
                stopwatch.Stop();
                await Task.Delay((int)Math.Max(delay - stopwatch.ElapsedMilliseconds, 0));
            }
        }
    }

    private double RenderNextFrame()
    {
        if (currentFrameIndex < frames.Count - 1)
        {
            currentFrameIndex++;
        }
        else
        {
            currentFrameIndex = 0;
            drawingSession.Clear(Colors.Transparent);
        }

        var frame = frames[currentFrameIndex];

        if (frame.RequiresClear)
        {
            drawingSession.Clear(Colors.Transparent);
        }

        drawingSession.DrawImage(frame.CanvasImage, (float)frame.Offset.X, (float)frame.Offset.Y);
        drawingSession.Flush();

        FrameRendered?.Invoke(this, EventArgs.Empty);

        return frame.Delay;
    }

}
