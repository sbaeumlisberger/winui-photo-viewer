using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Essentials.NET;
using LiteDB;
using MetadataAPI;
using MetadataAPI.Data;
using PeopleTagTool.Models;
using PeopleTagTool.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WIC;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;

namespace PeopleTagTool.ViewModels;

internal partial class PeopleTagPageModel : ObservableObject
{
    public class PersonNameSuggestion
    {
        public required string Name { get; init; }
        public DateTimeOffset LastUsed { get; set; } = DateTimeOffset.MinValue;
    }

    public string ProgressText { get => field; set => SetProperty(ref field, value); } = "";

    public ObservableList<DetectedFaceViewModel> DetectedFaces { get; } = [];

    public IReadOnlyCollection<DetectedFaceViewModel> SelecetedFaces { get; set; } = [];

    public double MinSize { get => field; set => SetProperty(ref field, value); } = 200;

    public ObservableList<string> PeopleNames { get; } = [];

    public string NameSearchText { get => field; set => SetProperty(ref field, value); } = "";

    private readonly DialogService dialogService;

    private readonly LiteDatabase db;

    private readonly ILiteCollection<IndexedPhoto> indexedPhotos;

    private readonly Debouncer refreshDebouncer;

    private double progressInPercent = 0;

    private List<PersonNameSuggestion> allPeopleNames = [];

    public PeopleTagPageModel(DialogService dialogService)
    {
        this.dialogService = dialogService;
        string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        db = new LiteDatabase(Path.Combine(documentsFolder, "PeopleTagTool.litedb"));
        indexedPhotos = db.GetCollection<IndexedPhoto>("photos");
        indexedPhotos.EnsureIndex(x => x.RelativePath);
        refreshDebouncer = new Debouncer(TimeSpan.FromSeconds(1), Refresh);
        Refresh();
        PropertyChanged += PeopleTagPageModel_PropertyChanged;
    }

    private void PeopleTagPageModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MinSize):
                refreshDebouncer.Invoke();
                break;
            case nameof(NameSearchText):
                UpdatePeopleNames();
                break;
        }
    }

    public void OnWindowClosed()
    {
        db.Checkpoint();
    }

    private void Refresh()
    {
        var allIndexedPhotos = indexedPhotos.FindAll();

        DetectedFaces.SyncWith(LoadDetectedFaces(allIndexedPhotos).ToList());

        allPeopleNames = allIndexedPhotos.SelectMany(x => x.PeopleTags)
            .Distinct()
            .Select(name => new PersonNameSuggestion() { Name = name })
            .ToList();

        UpdatePeopleNames();
    }

    private IEnumerable<DetectedFaceViewModel> LoadDetectedFaces(IEnumerable<IndexedPhoto> allIndexedPhotos)
    {
        foreach (var photo in allIndexedPhotos.OrderByDescending(photo => photo.DateTaken))
        {
            foreach (var face in photo.DetectedFaces)
            {
                if (face.Width > MinSize && face.Height > MinSize)
                {
                    yield return ToViewModel(face, photo);
                }
            }
        }
    }

    private void UpdatePeopleNames()
    {
        PeopleNames.SyncWith(allPeopleNames
            .Where(x => x.Name.Contains(NameSearchText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Name.StartsWith(NameSearchText, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.LastUsed)
            .ThenBy(x => x.Name)
            .Select(x => x.Name)
            .ToList());
    }

    public async Task TagSelectedFacesAsync(string name)
    {
        foreach (var group in SelecetedFaces.GroupBy(vm => vm.FilePath))
        {
            using var fileStream = File.Open(group.Key, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            var encoder = new MetadataEncoder(fileStream);
            var peopleTags = encoder.GetProperty(MetadataProperties.People);
            foreach (var face in group)
            {
                var faceRect = new FaceRect(face.FaceBoxInPercent.X, face.FaceBoxInPercent.Y, face.FaceBoxInPercent.Width, face.FaceBoxInPercent.Height);
                peopleTags.Add(new PeopleTag(name, faceRect));
            }
            encoder.SetProperty(MetadataProperties.People, peopleTags);
            await encoder.EncodeAsync();
            foreach (var face in group)
            {
                var photo = face.Photo;
                photo.PeopleTags = photo.PeopleTags.Append(name).ToArray();
                photo.DetectedFaces.Remove(face.FaceModel);
                indexedPhotos.Update(photo);
            }
        }
        DetectedFaces.RemoveRange(SelecetedFaces);
        SelecetedFaces = [];

        if (allPeopleNames.Find(x => x.Name == name) is { } suggestion)
        {
            suggestion.LastUsed = DateTimeOffset.Now;
        }
        else
        {
            allPeopleNames.Add(new PersonNameSuggestion() { Name = name, LastUsed = DateTimeOffset.Now });
        }
        UpdatePeopleNames();
    }

    [RelayCommand]
    private async Task DetectFacesAsync()
    {
        var synchronizationContext = SynchronizationContext.Current!;

        string? photosFolder = await dialogService.PickFolderAsync();

        if (photosFolder is null)
        {
            return;
        }

        Trace.WriteLine("Start detection ...");

        ProgressText = "0%".PadLeft(4, '\u2007');

        var filesExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
        };
        var options = new EnumerationOptions { RecurseSubdirectories = true };
        var files = new ConcurrentQueue<string>(Directory.EnumerateFiles(photosFolder, "*", options)
            .Where(filePath => filesExtensions.Contains(Path.GetExtension(filePath))));
        int total = files.Count;

        var wic = WICImagingFactory.Create();

        await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Task.Run(async () =>
        {
            var faceDetector = await FaceDetector.CreateAsync();

            while (files.TryDequeue(out var filePath))
            {
                string relativePath = Path.GetRelativePath(photosFolder, filePath);

                if (!indexedPhotos.Exists(x => x.RelativePath == relativePath))
                {
                    using var fileStream = File.OpenRead(filePath);
                    var decoder = wic.CreateDecoderFromStream(fileStream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
                    var codecInfo = decoder.GetDecoderInfo();
                    var frame = decoder.GetFrame(0);
                    var metadataQueryReader = frame.GetMetadataQueryReader();
                    var metadataReader = new MetadataReader(metadataQueryReader, codecInfo);

                    var keywords = metadataReader.GetProperty(MetadataProperties.Keywords);
                    var peopleTags = metadataReader.GetProperty(MetadataProperties.People);
                    var dateTaken = metadataReader.GetProperty(MetadataProperties.DateTaken);
                    frame.GetSize(out int imageWidth, out int imageHeight);

                    List<DetectedFaceModel> detectedFaces = [];

                    if (peopleTags.Count == 0)
                    {
                        var bitmapDecoder = await BitmapDecoder.CreateAsync(fileStream.AsRandomAccessStream());
                        var softwareBitmap = await bitmapDecoder.GetSoftwareBitmapAsync();

                        var faceDetectorResults = await faceDetector.DetectFacesAsync(softwareBitmap);

                        detectedFaces = faceDetectorResults.Select(ToModel).ToList();
                    }

                    var photo = new IndexedPhoto
                    {
                        FilePath = filePath,
                        RelativePath = relativePath,
                        Width = imageWidth,
                        Height = imageHeight,
                        Keywords = keywords,
                        DateTaken = dateTaken ?? DateTime.MinValue,
                        PeopleTags = peopleTags.Select(x => x.Name).ToArray(),
                        DetectedFaces = detectedFaces
                    };

                    indexedPhotos.Insert(photo);
                }

                int i = total - files.Count;
                if (i % 100 == 0 || i == total)
                {
                    Trace.WriteLine($"Processed: {i} / {total}");

                    double progressInPercent = Math.Round((i / (double)total) * 100);

                    synchronizationContext.Post(_ =>
                    {
                        if (progressInPercent > this.progressInPercent)
                        {
                            this.progressInPercent = progressInPercent;
                            string progressText = (progressInPercent + "%").PadLeft(4, '\u2007');
                            ProgressText = progressText;
                        }
                    }, null);
                }
            }
        })));

        ProgressText = "";

        Refresh();
    }

    private DetectedFaceViewModel ToViewModel(DetectedFaceModel face, IndexedPhoto photo)
    {
        return new DetectedFaceViewModel(IgnoreFace)
        {
            FaceModel = face,
            Photo = photo,
            FilePath = photo.FilePath,
            FaceBoxInPixels = new Rect(face.X, face.Y, face.Width, face.Height),
            FaceBoxInPercent = new Rect(
                face.X / (double)photo.Width,
                face.Y / (double)photo.Height,
                face.Width / (double)photo.Width,
                face.Height / (double)photo.Height)
        };
    }

    private void IgnoreFace(DetectedFaceViewModel viewModel)
    {
        var photo = viewModel.Photo;
        photo.DetectedFaces.Remove(viewModel.FaceModel);
        indexedPhotos.Update(photo);
        DetectedFaces.Remove(viewModel);
    }

    private static DetectedFaceModel ToModel(DetectedFace face)
    {
        return new DetectedFaceModel
        {
            X = face.FaceBox.X,
            Y = face.FaceBox.Y,
            Width = face.FaceBox.Width,
            Height = face.FaceBox.Height
        };
    }
}
