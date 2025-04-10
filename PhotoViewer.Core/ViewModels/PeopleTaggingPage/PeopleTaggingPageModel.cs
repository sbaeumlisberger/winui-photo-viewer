﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using MetadataAPI.Data;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Graphics.Imaging;
using Rect = Windows.Foundation.Rect;

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleTaggingPageModel : ViewModelBase
{
    public partial bool ShowNoMorePeopleDetectedMessage { get; private set; } = false;

    public ObservableList<DetectedFace> DetectedFaces { get; } = [];

    public partial IReadOnlyList<DetectedFace> SelectedFaces { get; set; } = [];

    public partial IReadOnlyList<string> RecentPeopleNames { get; set; } = [];

    public partial IReadOnlyList<string> AllPeopleNames { get; set; } = [];

    public partial string NameSearch { get; set; } = string.Empty;

    private readonly IFaceDetectionService faceDetectionService;

    private readonly IImageLoaderService imageLoaderService;

    private readonly ISuggestionsService peopleSuggestionsService;

    private readonly IMetadataService metadataService;

    private readonly CancellationTokenSource cancellationTokenSource = new();

    internal PeopleTaggingPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IFaceDetectionService faceDetectionService,
        IImageLoaderService imageLoaderService,
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

    protected override void OnCleanup()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        DetectedFaces.ForEach(face => face.SourceImage.Dispose());
    }

    [RelayCommand]
    private void GoBack()
    {
        Messenger.Send(new NavigateBackMessage());
    }

    [RelayCommand]
    private async Task NameSelectedAsync(string name)
    {
        try
        {
            foreach (var face in SelectedFaces)
            {
                var orientation = await metadataService.GetMetadataAsync(face.File, MetadataProperties.Orientation);
                var peopleTags = await metadataService.GetMetadataAsync(face.File, MetadataProperties.People);
                var faceRect = PeopleTagUtil.ToFaceRect(face.FaceRectInPercent, orientation);
                peopleTags = peopleTags.Append(new PeopleTag(name, faceRect)).ToList();
                await metadataService.WriteMetadataAsync(face.File, MetadataProperties.People, peopleTags);
                DetectedFaces.Remove(face);
                ShowNoMorePeopleDetectedMessage = DetectedFaces.Count == 0;
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
            var allPeopleNames = peopleSuggestionsService.GetAll(NameSearch);

            if (!string.IsNullOrEmpty(NameSearch) && !allPeopleNames.Contains(NameSearch))
            {
                allPeopleNames.Add(NameSearch);
            }

            AllPeopleNames = allPeopleNames;      
        }
    }

    private async Task InitalizeAsync(ApplicationSession session)
    {
        var files = session.Files
            .OfType<IBitmapFileInfo>()
            .Where(file => file.IsMetadataSupported)
            .ToList();

        Stopwatch stopwatch = Stopwatch.StartNew();
        await Parallel.ForEachAsync(files, cancellationTokenSource.Token, async (file, cancellationToken) =>
        {
            var peopleTags = await metadataService.GetMetadataAsync(file, MetadataProperties.People);
            cancellationToken.ThrowIfCancellationRequested();

            if (peopleTags.Count == 0)
            {
                await DetectFacesAsync(imageLoaderService, file, cancellationToken).ConfigureAwait(false);
            }
        });
        stopwatch.Stop();
        Log.Debug($"Completed Face Detection in {stopwatch.ElapsedMilliseconds} ms");

        ShowNoMorePeopleDetectedMessage = DetectedFaces.Count == 0;
    }

    private async Task DetectFacesAsync(IImageLoaderService imageLoaderService, IBitmapFileInfo bitmapFile, CancellationToken cancellationToken)
    {
        try
        {
            Log.Debug($"Detecting faces in {bitmapFile.FilePath}");

            var image = await imageLoaderService.LoadFromFileAsync(bitmapFile, cancellationToken).ConfigureAwait(false);

            if (image is not ICanvasBitmapImageModel canvasBitmapImageModel)
            {
                return;
            }

            var detectedFaces = await faceDetectionService.DetectFacesAsync(canvasBitmapImageModel, cancellationToken).ConfigureAwait(false);

            if (detectedFaces.Count == 0)
            {
                return;
            }

            foreach (var face in detectedFaces)
            {
                await ProcessDetectedFaceAsync(face, canvasBitmapImageModel.CanvasBitmap, bitmapFile, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to detect faces in {bitmapFile.FilePath}", ex);
        }
    }

    private async Task ProcessDetectedFaceAsync(DetectedFaceModel face, CanvasBitmap canvasBitmap, IBitmapFileInfo bitmapFile, CancellationToken cancellationToken)
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

        await DispatchAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            DetectedFaces.Add(new DetectedFace(faceRectInPercent, faceImage, bitmapFile, canvasBitmap));
        });
    }

    private Rect ToFaceRectInPercent(BitmapBounds rect, BitmapSize bitmapSize)
    {
        return new Rect(
            rect.X / (double)bitmapSize.Width,
            rect.Y / (double)bitmapSize.Height,
            rect.Width / (double)bitmapSize.Width,
            rect.Height / (double)bitmapSize.Height);
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
