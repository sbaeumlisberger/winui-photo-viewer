using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
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

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleTaggingPageModel : ViewModelBase
{
    public ObservableGroupedCollection<string, DetectedFace> DetectedFaces { get; } = [];

    public IReadOnlyList<DetectedFace> SelectedFaces { get; set; } = [];

    public IReadOnlyList<string> PeopleNames { get; set; } = [];

    public string NameSearch { get; set; } = string.Empty;

    private readonly IFaceDetectionService faceDetectionService;

    private readonly ICachedImageLoaderService imageLoaderService;

    private readonly ISuggestionsService peopleSuggestionsService;

    private readonly IMetadataService metadataService;

    private readonly FaceRecognitionService faceRecognitionService;

    internal PeopleTaggingPageModel(
        ApplicationSession session,
        IFaceDetectionService faceDetectionService,
        ICachedImageLoaderService imageLoaderService,
        ISuggestionsService peopleSuggestionsService,
        IMetadataService metadataService,
        FaceRecognitionService faceRecognitionService)
    {
        this.faceDetectionService = faceDetectionService;
        this.imageLoaderService = imageLoaderService;
        this.peopleSuggestionsService = peopleSuggestionsService;
        this.metadataService = metadataService;
        this.faceRecognitionService = faceRecognitionService;
        PeopleNames = peopleSuggestionsService.GetRecent().Union(peopleSuggestionsService.GetAll()).ToList();
        PropertyChanged += PeopleTaggingBatchViewPageModel_PropertyChanged;
        _ = InitalizeAsync(session);
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
                faceRecognitionService.Train(face.File, face.FaceRect, name);
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
                PeopleNames = peopleSuggestionsService.GetRecent().Union(peopleSuggestionsService.GetAll()).ToList();
            }
            else
            {
                PeopleNames = peopleSuggestionsService.FindMatches(NameSearch, [], int.MaxValue);
            }
        }
    }

    private async Task InitalizeAsync(ApplicationSession session)
    {
        var files = session.Files.OfType<IBitmapFileInfo>().ToList();

        Stopwatch stopwatch2 = Stopwatch.StartNew();
        await Parallel.ForEachAsync(files, async (file, _) =>
        {
            try
            {
                var peopleTags = await metadataService.GetMetadataAsync(file, MetadataProperties.People);
                if (peopleTags.Count == 0)
                {
                    //return;
                }
                Stopwatch stopwatch = Stopwatch.StartNew();
                Log.Debug($"Loading image {file.FilePath}");
                var image = await imageLoaderService.LoadFromFileAsync(file, CancellationToken.None) as ICanvasBitmapImageModel;
                stopwatch.Stop();
                Log.Debug($"Loaded image {file.FilePath} in {stopwatch.ElapsedMilliseconds} ms");
                if (image is not null)
                {
                    var canvasBitmap = image.CanvasBitmap;
                    var softwareBitmap = await ToSoftwareBitmap(canvasBitmap);
                    await DetectFacesAsync(file, canvasBitmap, softwareBitmap);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load image {file.FilePath}", ex);
            }
        });
        stopwatch2.Stop();
        Log.Debug($"Completed Face Detection in {stopwatch2.ElapsedMilliseconds} ms");
    }

    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    private async Task<SoftwareBitmap> ToSoftwareBitmap(CanvasBitmap canvasBitmap)
    {
        await semaphore.WaitAsync();
        try
        {
            return await SoftwareBitmap.CreateCopyFromSurfaceAsync(canvasBitmap);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task DetectFacesAsync(IBitmapFileInfo bitmapFile, CanvasBitmap canvasBitmap, SoftwareBitmap softwareBitmap)
    {
        try
        {
            Log.Debug($"Detecting faces in {bitmapFile.FilePath}");

            var detectedFaces = await faceDetectionService.DetectFacesAsync(softwareBitmap);

            var imageSize = new Size(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);

            foreach (var face in detectedFaces)
            {
                var faceBox = face.FaceBox;

                var faceRectInPercent = new FaceRect(
                    faceBox.X / imageSize.Width,
                    faceBox.Y / imageSize.Height,
                    faceBox.Width / imageSize.Width,
                    faceBox.Height / imageSize.Height);

                double extraFactor = 0.3;
                double extraLeft = Math.Min(faceBox.Width * extraFactor, faceBox.X);
                double extraTop = Math.Min(faceBox.Height * extraFactor, faceBox.Y);
                double extraRight = Math.Min(faceBox.Width * extraFactor, imageSize.Width - (faceBox.X + faceBox.Width));
                double extraBottom = Math.Min(faceBox.Height * extraFactor, imageSize.Height - (faceBox.Y + faceBox.Height));
                        
                var faceImage = new AtlasEffect()
                {
                    Source = canvasBitmap,
                    SourceRectangle = new Rect(
                        faceBox.X - extraLeft,
                        faceBox.Y - extraTop,
                        faceBox.Width + extraLeft + extraRight,
                        faceBox.Height + extraTop + extraBottom)
                };

                string recognizedName = faceRecognitionService.Predict(bitmapFile, faceBox) ?? "unknown";

                await DispatchAsync(() => DetectedFaces.AddItem(recognizedName, new DetectedFace(faceRectInPercent, faceBox, faceImage, bitmapFile, canvasBitmap, recognizedName)));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to detect faces in {bitmapFile.FilePath}", ex);
        }
    }

}
