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
using System.Diagnostics;
using PhotoViewer.App.Messages;

namespace PhotoViewer.Core.ViewModels;

public interface ITagPeopleToolModel : IViewModel
{
    Task InitializeAsync();

    bool IsEnabled { get; set; }

    IBitmapImageModel? BitmapImage { get; set; }

    float UIScaleFactor { get; set; }
}

public partial class TagPeopleToolModel : ViewModelBase, ITagPeopleToolModel
{
    public bool IsEnabled { get; set; } = false;

    public bool IsSelectionEnabled { get; private set; } = false;

    public IBitmapImageModel? BitmapImage { get; set; }

    public float UIScaleFactor { get; set; } = 1;

    public IReadOnlyList<PeopleTagViewModel> TaggedPeople { get; set; } = new List<PeopleTagViewModel>();

    public IObservableReadOnlyList<Rect> SuggestedFaces => suggestedFaces;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public Rect SelectionRect { get; set; } = Rect.Empty;

    private readonly ISuggestionsService suggestionsService;

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    private readonly IFaceDetectionService faceDetectionService;

    private readonly IBitmapFileInfo bitmapFile;

    private readonly ObservableList<Rect> suggestedFaces = new ObservableList<Rect>();

    private List<Rect> initalSuggestedFaces = new List<Rect>();

    internal TagPeopleToolModel(
        IBitmapFileInfo bitmapFile,
        IMessenger messenger,
        ISuggestionsService suggestionsService,
        IMetadataService metadataService,
        IDialogService dialogService,
        IFaceDetectionService faceDetectionService) : base(messenger)
    {
        this.suggestionsService = suggestionsService;
        this.metadataService = metadataService;
        this.dialogService = dialogService;
        this.faceDetectionService = faceDetectionService;
        this.bitmapFile = bitmapFile;
    }

    public async Task InitializeAsync()
    {
        Register<MetadataModifiedMessage>(Receive);
        Register<SetTagPeopleToolActiveMessage>(Receive);
        Register<BitmapModifiedMesssage>(Receive);
        IsSelectionEnabled = Messenger.Send(new IsTagPeopleToolActiveRequestMessage());
        await LoadTaggedPeopleAsync();
    }

    private void Receive(SetTagPeopleToolActiveMessage msg)
    {
        IsSelectionEnabled = msg.IsActive;
    }

    private async void Receive(MetadataModifiedMessage msg)
    {
        if (msg.Files.Contains(bitmapFile) && msg.MetadataProperty == MetadataProperties.People)
        {
            await LoadTaggedPeopleAsync();
        }
    }

    private async void Receive(BitmapModifiedMesssage msg)
    {
        if (msg.BitmapFile.Equals(bitmapFile))
        {
            await LoadTaggedPeopleAsync();
        }
    }

    partial void OnIsEnabledChanged()
    {
        if (IsEnabled)
        {
            TrySelectNextDetectedFace();
        }
        else
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRect = Rect.Empty;

            if (TaggedPeople.IsEmpty())
            {
                suggestedFaces.MatchTo(initalSuggestedFaces);
            }
            else
            {
                suggestedFaces.Clear();
            }
        }
    }

    async partial void OnIsSelectionEnabledChanged()
    {
        TaggedPeople.ForEach(peopleTagVM => peopleTagVM.IsVisible = IsSelectionEnabled);

        if (IsSelectionEnabled)
        {
            await ShowDetectedFacesAsync();
        }
        else
        {
            suggestedFaces.Clear();
            initalSuggestedFaces.Clear();
            AutoSuggestBoxText = string.Empty;
            SelectionRect = Rect.Empty;
        }
    }

    async partial void OnBitmapImageChanged()
    {
        if (IsSelectionEnabled)
        {
            await ShowDetectedFacesAsync();
        }
    }

    public void ExitPeopleTagging()
    {
        Messenger.Send(new SetTagPeopleToolActiveMessage(false));
    }

    public IReadOnlyList<string> FindSuggestions(string text)
    {
        return suggestionsService.FindMatches(text);
    }

    public IReadOnlyList<string> GetRecentSuggestions()
    {
        return suggestionsService.GetRecentSuggestions();
    }

    public void TrySelectNextDetectedFace()
    {
        if (suggestedFaces.Any())
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRect = suggestedFaces.First();
            suggestedFaces.RemoveAt(0);
        }
        else
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRect = Rect.Empty;
        }
    }

    public void OnUserStartedSelection()
    {
        SelectionRect = Rect.Empty;
        suggestedFaces.Clear();
    }

    public void OnUserEndedSelection(Rect selection)
    {
        SelectionRect = selection;
    }

    [RelayCommand]
    private async Task AddPersonAsync()
    {
        Debug.Assert(IsEnabled);

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
            suggestionsService.AddSuggestionAsync(personName).LogOnException();
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

    [RelayCommand]
    private async Task RemovePeopleTag(string personName)
    {
        try
        {
            var people = await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People);
            people = people.Where(peopleTag => peopleTag.Name != personName).ToList();
            await metadataService.WriteMetadataAsync(bitmapFile, MetadataProperties.People, people);
            Messenger.Send(new MetadataModifiedMessage(new[] { bitmapFile }, MetadataProperties.People));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to remove people tag from \"{bitmapFile.FilePath}\":", ex);
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.RemovePeopleTagErrorDialog_Title,
                Message = ex.Message,
            });
        }
    }

    private async Task LoadTaggedPeopleAsync()
    {
        TaggedPeople = (await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People))
             .Where(peopleTag => !peopleTag.Rectangle.IsEmpty)
             .Select(peopleTag => new PeopleTagViewModel(IsSelectionEnabled, peopleTag.Name, peopleTag.Rectangle.ToRect()))
             .ToList();
    }

    private async Task ShowDetectedFacesAsync()
    {
        if (TaggedPeople.Any()) { return; }

        try
        {
            if (BitmapImage is ICanvasBitmapImageModel bitmapImage)
            {
                var detectedFaces = await faceDetectionService.DetectFacesAsync(bitmapImage);

                if (!IsSelectionEnabled) { return; }

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

                initalSuggestedFaces = suggestedFaces.ToList();

                if (IsEnabled)
                {
                    TrySelectNextDetectedFace();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Could not detect faces for {bitmapFile.FileName}", ex);
        }
    }

    public override string ToString()
    {
        return nameof(TagPeopleToolModel) + "(" + bitmapFile.DisplayName + ")";
    }

}

