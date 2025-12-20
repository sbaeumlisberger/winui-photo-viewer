using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PeopleTagTool.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

namespace PeopleTagTool.Views;

public sealed partial class DetectedFaceView : UserControl
{
    private record class DecoderCacheEntry(IRandomAccessStream Stream, Task<BitmapDecoder> Decoder)
    {
        public int UsageCount { get; set; }
    }

    private static readonly OrderedDictionary<string, DecoderCacheEntry> decodersCache = new();

    private DetectedFaceViewModel? ViewModel { get; set; }

    private CancellationTokenSource cancellationTokenSource = new();

    public DetectedFaceView()
    {
        InitializeComponent();
        DataContextChanged += (s, e) => OnDataContextChanged();
        OnDataContextChanged();
    }

    private void OnDataContextChanged()
    {
        if (ViewModel is not null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new();
            image.Source = null;
        }

        ViewModel = (DetectedFaceViewModel?)DataContext;

        if (ViewModel is not null)
        {
            StartLoadImage(ViewModel, cancellationTokenSource.Token);
        }
    }

    private void StartLoadImage(DetectedFaceViewModel vm, CancellationToken cancellationToken)
    {
        var bitmap = new WriteableBitmap((int)(vm.FaceBoxInPixels.Width * 1.2), (int)(vm.FaceBoxInPixels.Height * 1.2));
        _ = DecodeAsync(vm, bitmap, cancellationToken);
        image.Source = bitmap;
    }

    private static async Task DecodeAsync(DetectedFaceViewModel vm, WriteableBitmap bitmap, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        if (cancellationToken.IsCancellationRequested) { return; }

        var decoder = await GetDecoderAsync(vm.FilePath, out var releaseDecoder);
        if (cancellationToken.IsCancellationRequested) { return; }

        try
        {
            uint x = (uint)Math.Max(0, vm.FaceBoxInPixels.X - vm.FaceBoxInPixels.Width * 0.1);
            uint y = (uint)Math.Max(0, vm.FaceBoxInPixels.Y - vm.FaceBoxInPixels.Height * 0.1);
            uint width = (uint)Math.Min(vm.FaceBoxInPixels.Width * 1.2, decoder.OrientedPixelWidth - x);
            uint height = (uint)Math.Min(vm.FaceBoxInPixels.Height * 1.2, decoder.OrientedPixelHeight - y);
            var transform = new BitmapTransform { Bounds = new BitmapBounds(x, y, width, height) };

            var pixels = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage);
            if (cancellationToken.IsCancellationRequested) { return; }

            pixels.DetachPixelData().CopyTo(bitmap.PixelBuffer);
            bitmap.Invalidate();
        }
        finally
        {
            releaseDecoder();
        }
    }

    private static Task<BitmapDecoder> GetDecoderAsync(string filePath, out Action releaseDecoder)
    {
        if (decodersCache.TryGetValue(filePath, out var cacheEntry))
        {
            cacheEntry.UsageCount++;
            releaseDecoder = () => cacheEntry.UsageCount--;
            return cacheEntry.Decoder;
        }
        else
        {
            var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).AsRandomAccessStream();
            var decoderTask = BitmapDecoder.CreateAsync(stream).AsTask();
            cacheEntry = new(stream, decoderTask);
            cacheEntry.UsageCount++;
            releaseDecoder = () => cacheEntry.UsageCount--;
            decodersCache.Add(filePath, cacheEntry);
            for (int i = 0; decodersCache.Count > 100 && i < decodersCache.Count; i++)
            {
                var entry = decodersCache.GetAt(i).Value;
                if (entry.UsageCount == 0)
                {
                    decodersCache.RemoveAt(i);
                    entry.Stream.Dispose();
                    i--;
                }
            }
            return decoderTask;
        }
    }
    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(ViewModel!.FilePath));
    }

    private void CopyPathButton_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel!.FilePath);
        Clipboard.SetContent(dataPackage);
    }

    private void IgnoreButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel!.IgnoreFace();
    }
}
