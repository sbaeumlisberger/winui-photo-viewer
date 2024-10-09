using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using System.Numerics;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.ViewModels;

public partial class EditImageOverlayModel : ViewModelBase
{
    public class EditSettings : ObservableObject
    {
        /// <summary>-100 to 100</summary>
        public double Brightness { get; set; }

        /// <summary>-100 to 100</summary>
        public double Highlights { get; set; }

        /// <summary>-100 to 100</summary>
        public double Shadows { get; set; }

        /// <summary>-100 to 100</summary>
        public double Contrast { get; set; }

        /// <summary>-100 to 100</summary>
        public double Saturation { get; set; }

        /// <summary>-100 to 100</summary>
        public double Temperature { get; set; }

        /// <summary>0 to 100</summary>
        public double Sharpen { get; set; }

        public bool HasChanges => Brightness != 0
                                  || Highlights != 0
                                  || Shadows != 0
                                  || Contrast != 0
                                  || Saturation != 0
                                  || Temperature != 0
                                  || Sharpen != 0;

        public void Reset()
        {
            Brightness = 0;
            Highlights = 0;
            Shadows = 0;
            Contrast = 0;
            Saturation = 0;
            Temperature = 0;
            Sharpen = 0;
        }
    }

    public bool IsVisible { get; set; }

    public IBitmapFileInfo? File { get; set; }

    public IBitmapImageModel? Image { get; set; }

    public EditSettings Settings { get; } = new EditSettings();

    public IBitmapImageModel? RenderResult { get; private set; }

    private readonly IDialogService dialogService;

    private readonly IMetadataService metadataService;

    private readonly Throttle renderThrottle;

    private CanvasRenderTarget? canvasRenderTarget;

    internal EditImageOverlayModel(IMessenger messenger, IDialogService dialogService, IMetadataService metadataService) : base(messenger)
    {
        this.dialogService = dialogService;
        this.metadataService = metadataService;

        renderThrottle = new Throttle(TimeSpan.FromMilliseconds(30), Render);

        PropertyChanged += EditImageOverlayModel_PropertyChanged;
        Settings.PropertyChanged += Settings_PropertyChanged;

        Register<BitmapImageLoadedMessage>(msg =>
        {
            if (msg.BitmapFile == File)
            {
                Image = msg.BitmapImage;
            }
        });

        Register<ToggleEditImageOverlayMessage>(msg => IsVisible = !IsVisible);
    }

    public async Task<bool> ConfirmCloseAsync()
    {
        if (!Settings.HasChanges)
        {
            return true;
        }

        var dialogModel = new MessageDialogModel()
        {
            Title = Strings.UnsavedChangesDialog_Title,
            Message = Strings.UnsavedChangesDialog_Message,
            PrimaryButtonText = Strings.UnsavedChangesDialog_CloseWithoutSaving,
            CloseButtonText = Strings.UnsavedChangesDialog_Cancel
        };

        await dialogService.ShowDialogAsync(dialogModel);

        return dialogModel.WasPrimaryButtonActivated;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAndReset();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await SaveReplaceAsync();
            Messenger.Send(new BitmapModifiedMesssage(File!));
            CloseAndReset();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to save image", ex);
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.SaveImageFailedDialog_Title,
                Message = ex.Message
            });
        }
    }

    [RelayCommand]
    private async Task SaveCopyAsync()
    {
        var fileSavePickerModel = new FileSavePickerModel()
        {
            SuggestedFileName = File!.FileName,
            FileTypeChoices = new() { { File.FileExtension.RemoveStart("."), [File.FileExtension] } }
        };

        await dialogService.ShowDialogAsync(fileSavePickerModel);

        if (fileSavePickerModel.File is { } dstFile)
        {
            try
            {
                if (dstFile.IsSameFile(File.StorageFile))
                {
                    await SaveReplaceAsync();
                    Messenger.Send(new BitmapModifiedMesssage(File));
                }
                else
                {
                    await SaveCopyAsnc(dstFile);
                }

                CloseAndReset();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save image", ex);
                await dialogService.ShowDialogAsync(new MessageDialogModel()
                {
                    Title = Strings.SaveImageFailedDialog_Title,
                    Message = ex.Message
                });
            }
        }
    }

    private void CloseAndReset()
    {
        IsVisible = false;
        Settings.Reset();
    }

    private void EditImageOverlayModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Image):
                canvasRenderTarget = null;
                renderThrottle.Invoke();
                break;
            case nameof(IsVisible):
                renderThrottle.Invoke();
                break;
        }
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        renderThrottle.Invoke();
    }

    private void Render()
    {
        if (Image is null || !IsVisible)
        {
            return;
        }

        ICanvasImage effect = ApplyEffects(Image.CanvasImage);

        canvasRenderTarget ??= new CanvasRenderTarget(Image.Device, Image.SizeInPixels.Width, Image.SizeInPixels.Height, 96);

        using (var drawingSession = canvasRenderTarget.CreateDrawingSession())
        {
            drawingSession.Clear(Colors.Transparent);
            drawingSession.DrawImage(effect);
        }

        RenderResult = new CanvasBitmapImageModel(Guid.NewGuid().ToString(), canvasRenderTarget, Image.ColorSpace);
    }

    private ICanvasImage ApplyEffects(ICanvasImage image)
    {
        ICanvasImage result = image;

        if (Settings.Brightness != 0)
        {
            result = new BrightnessEffect()
            {
                Source = result,
                WhitePoint = Settings.Brightness > 0
                    ? new Vector2(1 - (float)Settings.Brightness / 100f, 1)
                    : new Vector2(1, 1 - Math.Abs((float)Settings.Brightness / 100f)),
                BlackPoint = Settings.Brightness > 0
                    ? new Vector2(0, (float)Settings.Brightness / 100f)
                    : new Vector2(Math.Abs((float)Settings.Brightness / 100f), 0)
            };
        }

        if (Settings.Highlights != 0 || Settings.Shadows != 0)
        {
            result = new HighlightsAndShadowsEffect()
            {
                Source = result,
                Highlights = (float)Settings.Highlights / 100f,
                Shadows = (float)Settings.Shadows / 100f,
            };
        }

        if (Settings.Contrast != 0)
        {
            result = new ContrastEffect()
            {
                Source = result,
                Contrast = (float)Settings.Contrast / 100f
            };
        }

        if (Settings.Saturation != 0)
        {
            result = new SaturationEffect()
            {
                Source = result,
                Saturation = (float)Settings.Saturation / 100f + 1
            };
        }

        if (Settings.Temperature != 0)
        {
            result = new TemperatureAndTintEffect()
            {
                Source = result,
                Temperature = (float)Settings.Temperature / 100f
            };
        }

        if (Settings.Sharpen != 0)
        {
            result = new SharpenEffect()
            {
                Source = result,
                Amount = (float)Settings.Sharpen / 10
            };
        }

        return result;
    }

    private async Task SaveCopyAsnc(IStorageFile dstFile)
    {
        using var srcStream = await File!.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);
        using var dstStream = await dstFile.OpenAsync(FileAccessMode.ReadWrite);

        await TranscodeAsync(srcStream, dstStream);
    }

    private async Task SaveReplaceAsync()
    {
        using var fileStream = await File!.OpenAsRandomAccessStreamAsync(FileAccessMode.ReadWrite);
        using var memoryStream = new InMemoryRandomAccessStream();

        await TranscodeAsync(fileStream, memoryStream);

        memoryStream.Seek(0);
        fileStream.Seek(0);
        fileStream.Size = 0;
        await RandomAccessStream.CopyAsync(memoryStream, fileStream);

        metadataService.InvalidateCache(File);
    }

    private async Task TranscodeAsync(IRandomAccessStream srcStream, IRandomAccessStream dstStream)
    {
        var metadataDecoder = new MetadataDecoder(srcStream.AsStream());
        var oldOrientation = metadataDecoder.GetProperty(MetadataProperties.Orientation);

        srcStream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(srcStream);

        dstStream.Seek(0);
        dstStream.Size = 0;
        var encoder = await BitmapEncoder.CreateForTranscodingAsync(dstStream, decoder);

        if (encoder.EncoderInformation.CodecId == BitmapEncoder.JpegEncoderId
            || encoder.EncoderInformation.CodecId == BitmapEncoder.TiffEncoderId)
        {
            encoder.IsThumbnailGenerated = true;
            double aspectRadio = decoder.PixelWidth / (double)decoder.PixelHeight;
            encoder.GeneratedThumbnailWidth = (uint)(256 * Math.Min(aspectRadio, 1));
            encoder.GeneratedThumbnailHeight = (uint)(256 / Math.Max(aspectRadio, 1));
        }

        SetPixelData(encoder, canvasRenderTarget!);

        await encoder.FlushAsync();
        await dstStream.FlushAsync();

        dstStream.Seek(0);
        await RemoveOrientationFlag(dstStream.AsStream(), oldOrientation);
    }

    private static async Task RemoveOrientationFlag(Stream stream, PhotoOrientation oldOrientation)
    {
        var metadataEncoder = new MetadataEncoder(stream);

        if (!MetadataProperties.Orientation.SupportedFormats.Contains(metadataEncoder.CodecInfo.GetContainerFormat()))
        {
            return;
        }

        metadataEncoder.SetProperty(MetadataProperties.Orientation, PhotoOrientation.Normal);

        var peopleTags = metadataEncoder.GetProperty(MetadataProperties.People);
        for (int i = peopleTags.Count - 1; i >= 0; i--)
        {
            PeopleTag peopleTag = peopleTags[i];

            if (peopleTag.Rectangle is not null)
            {
                var roatatedRect = ReverseRotateRect(peopleTag.Rectangle.Value, oldOrientation);
                peopleTags[i] = new PeopleTag(peopleTag.Name, roatatedRect, peopleTag.EmailDigest, peopleTag.LiveCID);
            }
        }
        metadataEncoder.SetProperty(MetadataProperties.People, peopleTags);

        await metadataEncoder.EncodeAsync();
        stream.Flush();
    }

    private static FaceRect ReverseRotateRect(FaceRect rect, PhotoOrientation orientation)
    {
        switch (orientation)
        {
            case PhotoOrientation.Normal:
            case PhotoOrientation.Unspecified:
                return rect;
            case PhotoOrientation.Rotate90:
                return new FaceRect(rect.Y, 1 - rect.X - rect.Width, rect.Height, rect.Width);
            case PhotoOrientation.Rotate180:
                return new FaceRect(1 - rect.X - rect.Width, 1 - rect.Y - rect.Height, rect.Width, rect.Height);
            case PhotoOrientation.Rotate270:
                return new FaceRect(1 - rect.Y - rect.Height, rect.X, rect.Height, rect.Width);
            default:
                throw new NotSupportedException("Unsupported orientation: " + orientation);
        }
    }

    private static void SetPixelData(BitmapEncoder encoder, CanvasBitmap bitmap)
    {
        encoder.SetPixelData(
            (BitmapPixelFormat)bitmap.Format,
            (BitmapAlphaMode)bitmap.AlphaMode,
            bitmap.SizeInPixels.Width,
            bitmap.SizeInPixels.Height,
            bitmap.Dpi,
            bitmap.Dpi,
            bitmap.GetPixelBytes());
    }
}
