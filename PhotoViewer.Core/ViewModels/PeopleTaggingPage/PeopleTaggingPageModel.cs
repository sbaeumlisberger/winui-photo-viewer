using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Runtime.Intrinsics.Arm;
using System.IO;
using OpenCvSharp;
using Windows.Graphics.DirectX;
using Rect = Windows.Foundation.Rect;
using System.Runtime.InteropServices;
using System.Collections;
using Size = Windows.Foundation.Size;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleTaggingPageModel : ViewModelBase
{
    public ObservableGroupedCollection<string, DetectedFace> DetectedFaces { get; } = [];

    public IReadOnlyList<DetectedFace> SelectedFaces { get; set; } = [];

    public IReadOnlyList<string> RecentPeopleNames { get; set; } = [];

    public IReadOnlyList<string> AllPeopleNames { get; set; } = [];

    public string NameSearch { get; set; } = string.Empty;

    private readonly IFaceDetectionService faceDetectionService;

    private readonly ICachedImageLoaderService imageLoaderService;

    private readonly ISuggestionsService peopleSuggestionsService;

    private readonly IMetadataService metadataService;

    private readonly FaceRecognitionService faceRecognitionService;

    internal PeopleTaggingPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IFaceDetectionService faceDetectionService,
        ICachedImageLoaderService imageLoaderService,
        ISuggestionsService peopleSuggestionsService,
        IMetadataService metadataService,
        FaceRecognitionService faceRecognitionService)
        : base(messenger)
    {
        this.faceDetectionService = faceDetectionService;
        this.imageLoaderService = imageLoaderService;
        this.peopleSuggestionsService = peopleSuggestionsService;
        this.metadataService = metadataService;
        this.faceRecognitionService = faceRecognitionService;
        RecentPeopleNames = peopleSuggestionsService.GetRecent();
        AllPeopleNames = peopleSuggestionsService.GetAll();
        PropertyChanged += PeopleTaggingBatchViewPageModel_PropertyChanged;
        _ = InitalizeAsync(session);
    }

    [RelayCommand]
    private void GoBack()
    {
        Messenger.Send(new NavigateBackMessage());
    }

    [RelayCommand]
    private async Task NameSelected(string name)
    {
        try
        {
            foreach (var face in SelectedFaces)
            {
                var peopleTags = await metadataService.GetMetadataAsync(face.File, MetadataProperties.People);
                peopleTags.Append(new PeopleTag(name, face.FaceRectInPercent)).ToArray();
                await metadataService.WriteMetadataAsync(face.File, MetadataProperties.People, peopleTags);
                DetectedFaces.FirstGroupByKey(face.RecognizedName).Remove(face);
                //faceRecognitionService.Train(face.File, face.FaceRect, name);
                await peopleSuggestionsService.AddSuggestionAsync(name);
                RecentPeopleNames = peopleSuggestionsService.GetRecent();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to write metadata", ex);
        }
    }

    private void PeopleTaggingBatchViewPageModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NameSearch))
        {
            if (string.IsNullOrEmpty(NameSearch))
            {
                AllPeopleNames = peopleSuggestionsService.GetAll();
            }
            else
            {
                AllPeopleNames = peopleSuggestionsService.FindMatches(NameSearch, [], int.MaxValue);
            }
        }
    }

    private async Task InitalizeAsync(ApplicationSession session)
    {
        var files = session.Files.OfType<IBitmapFileInfo>().ToList();

        Stopwatch stopwatch = Stopwatch.StartNew();
        await Parallel.ForEachAsync(files, async (file, _) =>
        {
            var peopleTags = await metadataService.GetMetadataAsync(file, MetadataProperties.People);
            if (peopleTags.Count != 0)
            {
                await DetectFacesAsync(file);
            }
        });
        stopwatch.Stop();
        Log.Debug($"Completed Face Detection in {stopwatch.ElapsedMilliseconds} ms");
    }

    private async Task DetectFacesAsync(IBitmapFileInfo bitmapFile)
    {
        try
        {
            Log.Debug($"Detecting faces in {bitmapFile.FilePath}");

            var softwareBitmap = await TryLoadSoftwareBitmapAsync(bitmapFile);

            if (softwareBitmap is null)
            {
                return;
            }

            var detectedFaces = await faceDetectionService.DetectFacesAsync(softwareBitmap);

            if (detectedFaces.Count == 0)
            {
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice(), softwareBitmap);
            stopwatch.Stop();
            Log.Debug($"CanvasBitmap.CreateFromSoftwareBitmap took {stopwatch.ElapsedMilliseconds} ms");

            var sizeInPixels = canvasBitmap.SizeInPixels;

            foreach (var face in detectedFaces)
            {
                var faceBox = face.FaceBox;

                var faceRectInPercent = new FaceRect(
                    faceBox.X / sizeInPixels.Width,
                    faceBox.Y / sizeInPixels.Height,
                    faceBox.Width / sizeInPixels.Width,
                    faceBox.Height / sizeInPixels.Height);

                double extraFactor = 0.3;
                double extraLeft = Math.Min(faceBox.Width * extraFactor, faceBox.X);
                double extraTop = Math.Min(faceBox.Height * extraFactor, faceBox.Y);
                double extraRight = Math.Min(faceBox.Width * extraFactor, sizeInPixels.Width - (faceBox.X + faceBox.Width));
                double extraBottom = Math.Min(faceBox.Height * extraFactor, sizeInPixels.Height - (faceBox.Y + faceBox.Height));

                Rect extraRectInPixels = new Rect(
                        faceBox.X - extraLeft,
                        faceBox.Y - extraTop,
                        faceBox.Width + extraLeft + extraRight,
                        faceBox.Height + extraTop + extraBottom);

                Rect extraRectInDIPs = new Rect(
                    canvasBitmap.ConvertPixelsToDips((int)extraRectInPixels.X),
                    canvasBitmap.ConvertPixelsToDips((int)extraRectInPixels.Y),
                    canvasBitmap.ConvertPixelsToDips((int)extraRectInPixels.Width),
                    canvasBitmap.ConvertPixelsToDips((int)extraRectInPixels.Height));

                var faceImage = new AtlasEffect()
                {
                    Source = canvasBitmap,
                    SourceRectangle = extraRectInDIPs,
                };

                string recognizedName = "unknown";// faceRecognitionService.Predict(bitmapFile, faceBox) ?? "unknown";

                await DispatchAsync(() => DetectedFaces.AddItem(recognizedName, new DetectedFace(faceRectInPercent, faceBox, faceImage, bitmapFile, canvasBitmap, recognizedName)));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to detect faces in {bitmapFile.FilePath}", ex);
        }
    }

    private async Task<SoftwareBitmap?> TryLoadSoftwareBitmapAsync(IBitmapFileInfo bitmapFile)
    {
        try
        {
            // TODO use opencv to speed up?

            Stopwatch stopwatch = Stopwatch.StartNew();
            Log.Debug($"Loading image {bitmapFile.FilePath}");
            using var fileStream = await bitmapFile.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);

            // read file at once into memory to prevent slow parallel file access
            InMemoryRandomAccessStream memoryStream = new();
            await RandomAccessStream.CopyAsync(fileStream, memoryStream);
            fileStream.Dispose();
            memoryStream.Seek(0);

            var bitmapDecoder = await BitmapDecoder.CreateAsync(memoryStream);
            // pixel format and alpha mode mus be compatible with CanvasBitmap.CreateFromSoftwareBitmap
            // for pixel format see: https://microsoft.github.io/Win2D/WinUI2/html/M_Microsoft_Graphics_Canvas_CanvasBitmap_CreateFromSoftwareBitmap.htm
            // for alpha mode see: https://microsoft.github.io/Win2D/WinUI2/html/PixelFormats.htm
            var softwareBitmap = await bitmapDecoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
            stopwatch.Stop();
            Log.Debug($"Loaded image {bitmapFile.FilePath} in {stopwatch.ElapsedMilliseconds} ms");
            return softwareBitmap;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load image {bitmapFile.FilePath}", ex);
            return null;
        }
    }

}
