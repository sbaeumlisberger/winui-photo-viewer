using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using OpenCvSharp;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Rect = Windows.Foundation.Rect;

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleTaggingPageModel : ViewModelBase
{
    public ObservableList<DetectedFace> DetectedFaces { get; } = [];

    public IReadOnlyList<DetectedFace> SelectedFaces { get; set; } = [];

    public IReadOnlyList<string> RecentPeopleNames { get; set; } = [];

    public IReadOnlyList<string> AllPeopleNames { get; set; } = [];

    public string NameSearch { get; set; } = string.Empty;

    private readonly IFaceDetectionService faceDetectionService;

    private readonly ICachedImageLoaderService imageLoaderService;

    private readonly ISuggestionsService peopleSuggestionsService;

    private readonly IMetadataService metadataService;

    internal PeopleTaggingPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IFaceDetectionService faceDetectionService,
        ICachedImageLoaderService imageLoaderService,
        ISuggestionsService peopleSuggestionsService,
        IMetadataService metadataService)
        : base(messenger)
    {
        this.faceDetectionService = faceDetectionService;
        this.imageLoaderService = imageLoaderService;
        this.peopleSuggestionsService = peopleSuggestionsService;
        this.metadataService = metadataService;
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
                DetectedFaces.Remove(face);
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
        var files = session.Files
            .OfType<IBitmapFileInfo>()
            .Where(file => file.IsMetadataSupported)
            .ToList();

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

            using Mat matBGRA8 = LoadImageAsMatBGRA8(bitmapFile);

            var softwareBitmap = ConvertMatBGRA8toSoftwareBitmap(matBGRA8);

            var detectedFaces = await faceDetectionService.DetectFacesAsync(softwareBitmap);

            if (detectedFaces.Count == 0)
            {
                return;
            }

            //Stopwatch stopwatch = Stopwatch.StartNew();
            var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice(), softwareBitmap);
            //stopwatch.Stop();
            //Log.Debug($"CanvasBitmap.CreateFromSoftwareBitmap took {stopwatch.ElapsedMilliseconds} ms");

            foreach (var face in detectedFaces)
            {
                await ProcessDetectedFaceAsync(face, canvasBitmap, bitmapFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to detect faces in {bitmapFile.FilePath}", ex);
        }
    }

    private async Task ProcessDetectedFaceAsync(DetectedFaceModel face, CanvasBitmap canvasBitmap, IBitmapFileInfo bitmapFile)
    {
        var sizeInPixels = canvasBitmap.SizeInPixels;

        var faceBoxInPixels = face.FaceBox;

        var faceRectInPercent = ToFaceRectInPercent(faceBoxInPixels, sizeInPixels);

        var extendedFaceBoxInPixels = ExtendFaceBox(faceBoxInPixels, 0.3, sizeInPixels);
        var extendedFaceBoxInDIPs = ConvertFromPixelsToDIPs(extendedFaceBoxInPixels, canvasBitmap);

        var faceImage = new AtlasEffect()
        {
            Source = canvasBitmap,
            SourceRectangle = extendedFaceBoxInDIPs,
        };

        await DispatchAsync(() => DetectedFaces.Add(new DetectedFace(faceRectInPercent, faceImage, bitmapFile, canvasBitmap)));
    }

    private Mat LoadImageAsMatBGRA8(IBitmapFileInfo bitmapFile)
    {
        //Stopwatch stopwatch = Stopwatch.StartNew();
        //Log.Debug($"Loading image {bitmapFile.FilePath}");

        using Mat matBGR8 = Cv2.ImRead(bitmapFile.FilePath, ImreadModes.Color);
        Mat matBGRA8 = new Mat(matBGR8.Width, matBGR8.Height, MatType.CV_8UC4);
        Cv2.CvtColor(matBGR8, matBGRA8, ColorConversionCodes.BGR2BGRA);

        //stopwatch.Stop();
        //Log.Debug($"Loaded image {bitmapFile.FilePath} in {stopwatch.ElapsedMilliseconds} ms");
        return matBGRA8;
    }

    private SoftwareBitmap ConvertMatBGRA8toSoftwareBitmap(Mat matBGRA8)
    {
        //Stopwatch stopwatch = Stopwatch.StartNew();

        byte[] pixelBytes = new byte[matBGRA8.Total() * matBGRA8.ElemSize()];
        Marshal.Copy(matBGRA8.Data, pixelBytes, 0, pixelBytes.Length);

        var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(pixelBytes.AsBuffer(), BitmapPixelFormat.Bgra8, matBGRA8.Width, matBGRA8.Height, BitmapAlphaMode.Ignore);

        //stopwatch.Stop();
        //Log.Debug($"Create SoftwareBitmap took {stopwatch.ElapsedMilliseconds} ms");

        return softwareBitmap;
    }

    private FaceRect ToFaceRectInPercent(BitmapBounds rect, BitmapSize bitmapSize)
    {
        return new FaceRect(
            rect.X / bitmapSize.Width,
            rect.Y / bitmapSize.Height,
            rect.Width / bitmapSize.Width,
            rect.Height / bitmapSize.Height);
    }

    private Rect ExtendFaceBox(BitmapBounds faceBox, double factor, BitmapSize imageSize)
    {
        double extendedX = Math.Max(faceBox.X - faceBox.Width * factor, 0);
        double extendedY = Math.Max(faceBox.Y - faceBox.Height * factor, 0);
        double extendedWidth = Math.Min(faceBox.Width + (faceBox.X - extendedX) + faceBox.Width * factor, imageSize.Width - extendedX);
        double extendedHeight = Math.Min(faceBox.Height + (faceBox.Y - extendedY) + faceBox.Height * factor, imageSize.Height - extendedY);
        return new Rect(extendedX, extendedY, extendedWidth, extendedHeight);
    }

    private Rect ConvertFromPixelsToDIPs(Rect rect, CanvasBitmap canvasBitmap)
    {
        return new Rect(
            canvasBitmap.ConvertPixelsToDips((int)rect.X),
            canvasBitmap.ConvertPixelsToDips((int)rect.Y),
            canvasBitmap.ConvertPixelsToDips((int)rect.Width),
            canvasBitmap.ConvertPixelsToDips((int)rect.Height));
    }
}
