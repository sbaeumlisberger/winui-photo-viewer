using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using PhotoViewer.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PhotoViewer.App.Utils;

public sealed class AnimatedBitmapRenderer : IDisposable
{
    public event TypedEventHandler<AnimatedBitmapRenderer, EventArgs>? FrameRendered;

    public CanvasRenderTarget RenderTarget => renderTarget ?? throw new ObjectDisposedException(GetType().FullName);

    public bool IsPlaying
    {
        get;
        set
        {
            ObjectDisposedException.ThrowIf(renderTarget is null, this);

            if (value != field)
            {
                field = value;

                if (field)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    _ = RenderAsync(cancellationTokenSource.Token);
                }
                else
                {
                    cancellationTokenSource?.Cancel();
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
            }
        }
    }

    private readonly IReadOnlyList<IBitmapFrameModel> frames;

    private CanvasRenderTarget? renderTarget;

    private int currentFrameIndex = 0;

    private CancellationTokenSource? cancellationTokenSource;

    public AnimatedBitmapRenderer(IBitmapImageModel animatedBitmap)
    {
        frames = animatedBitmap.Frames;

        var device = animatedBitmap.Device;
        var size = animatedBitmap.SizeInPixels;

        renderTarget = new CanvasRenderTarget(device, size.Width, size.Height, 96);

        RenderCurrentFrame(renderTarget);
    }

    public void Dispose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        FrameRendered = null;
        renderTarget?.Dispose();
        renderTarget = null;
    }

    private async Task RenderAsync(CancellationToken cancellationToken)
    {
        if (frames.Count <= 1)
        {
            return;
        }

        while (true)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            RenderCurrentFrame(renderTarget!);
            FrameRendered?.Invoke(this, EventArgs.Empty);
            var elapsedTime = Stopwatch.GetElapsedTime(startTimestamp);

            var delay = CalucateDelay(elapsedTime);
            await Task.Delay(delay, cancellationToken).SuppressThrowing();
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            MoveToNextFrame();
        }
    }

    private void RenderCurrentFrame(CanvasRenderTarget renderTarget)
    {
        using var drawingSession = renderTarget.CreateDrawingSession();

        var frame = frames[currentFrameIndex];

        if (currentFrameIndex == 0 || frame.RequiresClear)
        {
            drawingSession.Clear(Colors.Transparent);
        }

        drawingSession.DrawImage(frame.CanvasImage, (float)frame.Offset.X, (float)frame.Offset.Y);
        drawingSession.Flush();
    }

    private TimeSpan CalucateDelay(TimeSpan elapsedTime)
    {
        var delay = frames[currentFrameIndex].Delay - elapsedTime;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    private void MoveToNextFrame()
    {
        if (currentFrameIndex < frames.Count - 1)
        {
            currentFrameIndex++;
        }
        else
        {
            currentFrameIndex = 0;
        }
    }

}
