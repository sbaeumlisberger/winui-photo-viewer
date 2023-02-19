using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.ViewModels;

public partial class TagPeopleToolModel : ViewModelBase
{
    public bool IsEnabeld { get; set; } = false;

    public bool IsActive { get; private set; } = false;

    public IBitmapImage? BitmapImage { get; set; }

    public float UIScaleFactor { get; set; } = 1;

    public IReadOnlyList<PeopleTagViewModel> TaggedPeople { get; set; } = new List<PeopleTagViewModel>();

    public IObservableReadOnlyList<Rect> SuggestedFaces => suggestedFaces;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public Rect SelectionRect { get; set; }

    private readonly ISuggestionsService suggestionsService;

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    private readonly IBitmapFileInfo bitmapFile;

    private readonly ObservableList<Rect> suggestedFaces = new ObservableList<Rect>();

    internal TagPeopleToolModel(
        IBitmapFileInfo bitmapFile,
        IMessenger messenger,
        ISuggestionsService suggestionsService,
        IMetadataService metadataService,
        IDialogService dialogService) : base(messenger)
    {
        this.suggestionsService = suggestionsService;
        this.metadataService = metadataService;
        this.dialogService = dialogService;
        this.bitmapFile = bitmapFile;
    }

    protected override async void OnViewConnectedOverride()
    {
        Messenger.Register<TagPeopleToolActiveChangedMeesage>(this, Receive);
        Messenger.Register<MetadataModifiedMessage>(this, Receive);
        TaggedPeople = await LoadTaggedPeopleAsync();
    }

    private void Receive(TagPeopleToolActiveChangedMeesage msg)
    {
        IsActive = msg.IsActive;
    }

    private async void Receive(MetadataModifiedMessage msg)
    {
        if (msg.Files.Contains(bitmapFile) && msg.MetadataProperty == MetadataProperties.People)
        {
            TaggedPeople = await LoadTaggedPeopleAsync();
        }
    }

    partial void OnIsEnabeldChanged()
    {
        if (IsEnabeld)
        {
            TrySelectNextDetectedFace();
        }
        else
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRect = default;
        }
    }

    async partial void OnIsActiveChanged()
    {
        TaggedPeople.ForEach(peopleTagVM => peopleTagVM.IsVisible = IsActive);

        if (IsActive)
        {
            await ShowDetectedFacesAsync();
        }
        else
        {
            suggestedFaces.Clear();
            AutoSuggestBoxText = string.Empty;
            SelectionRect = default;
        }
    }

    async partial void OnBitmapImageChanged()
    {
        if (IsActive)
        {
            await ShowDetectedFacesAsync();
        }
    }

    public void ExitPeopleTagging()
    {
        Messenger.Send(new TagPeopleToolActiveChangedMeesage(false));
    }

    public IReadOnlyList<string> FindSuggestions(string text)
    {
        return suggestionsService.FindMatches(text);
    }

    public IReadOnlyList<string> GetRecentSuggestions()
    {
        return suggestionsService.GetRecentSuggestions();
    }

    public bool SkipCurrentDetectedFace()
    {
        if (TrySelectNextDetectedFace())
        {
            return true;
        }
        else
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRect = default;
            return false;
        }
    }

    public void ClearSuggestedFaces()
    {
        suggestedFaces.Clear();
    }

    [RelayCommand]
    private async Task AddPersonAsync()
    {
        string personName = AutoSuggestBoxText.Trim();

        if (personName == string.Empty) 
        {
            return;
        }

        var people = await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People);

        if (people.Any(peopleTag => peopleTag.Name == personName))
        {
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.PeopleTagAlreadyExistingDialog_Title,
                Message = string.Format(Strings.PeopleTagAlreadyExistingDialog_Message, personName),
            });
            return;
        }

        var faceRect = new FaceRect(SelectionRect.X, SelectionRect.Y,
                                    SelectionRect.Width, SelectionRect.Height);

        people.Add(new PeopleTag(personName, faceRect));

        try
        {
            await metadataService.WriteMetadataAsync(bitmapFile, MetadataProperties.People, people);
            Messenger.Send(new MetadataModifiedMessage(new[] { bitmapFile }, MetadataProperties.People));
            _ = suggestionsService.AddSuggestionAsync(personName);
            AutoSuggestBoxText = string.Empty;
            SelectionRect = default;
            TrySelectNextDetectedFace();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to add people tag to \"{bitmapFile.FilePath}\":", ex);
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.AddPeopleTagErrorDialog_Title,
                Message = ex.Message,
            });
        }
    }

    private async Task<List<PeopleTagViewModel>> LoadTaggedPeopleAsync()
    {
        return (await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People))
             .Where(peopleTag => !peopleTag.Rectangle.IsEmpty)
             .Select(peopleTag => new PeopleTagViewModel(IsActive, peopleTag.Name, peopleTag.Rectangle))
             .ToList();
    }

    private async Task ShowDetectedFacesAsync()
    {
        if (TaggedPeople.Any()) { return; }

        try
        {
            if (BitmapImage is PVBitmapImage bitmapImage)
            {
                var detectedFaces = await DetectFacesAsync(bitmapImage, CancellationToken.None/*TODO?*/);

                var imageSize = bitmapImage.SizeInDIPs;

                foreach (var face in detectedFaces)
                {
                    var faceBox = face.FaceBox;

                    suggestedFaces.Add(new Rect(
                        faceBox.X / imageSize.Width,
                        faceBox.Y / imageSize.Height,
                        faceBox.Width / imageSize.Width,
                        faceBox.Height / imageSize.Height));
                }

                if (IsEnabeld)
                {
                    TrySelectNextDetectedFace();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // operation canceled
        }
        catch (Exception ex)
        {
            Log.Error($"Could not detect faces for {bitmapFile.FileName}", ex);
        }
    }

    private async Task<List<DetectedFace>> DetectFacesAsync(PVBitmapImage bitmapImage, CancellationToken cancellationToken)
    {
        if (!FaceDetector.IsSupported)
        {
            Log.Info("Could not detect faces: FaceDetector not supported on this device.");
            return new List<DetectedFace>();
        }

        var faceDetector = await FaceDetector.CreateAsync().AsTask(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(bitmapImage.CanvasBitmap).AsTask(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var softwareBitmapGray8 = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Gray8);

        var detectedFaces = await faceDetector.DetectFacesAsync(softwareBitmapGray8).AsTask(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return detectedFaces.OrderBy(face => face.FaceBox.X).ToList();
    }

    private bool TrySelectNextDetectedFace()
    {
        if (suggestedFaces.Any())
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRect = suggestedFaces.First();
            suggestedFaces.RemoveAt(0);
            return true;
        }
        return false;
    }

}

