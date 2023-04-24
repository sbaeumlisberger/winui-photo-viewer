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

public interface ITagPeopleToolModel
{
    Task InitializeAsync();

    void Cleanup();

    bool IsEnabled { get; set; }

    IBitmapImageModel? BitmapImage { get; set; }

    float UIScaleFactor { get; set; }
}

public partial class TagPeopleToolModel : ViewModelBase, ITagPeopleToolModel
{
    public bool IsEnabled { get; set; } = false;

    public bool IsActive { get; private set; } = false;

    public IBitmapImageModel? BitmapImage { get; set; }

    public float UIScaleFactor { get; set; } = 1;

    public IReadOnlyList<PeopleTagViewModel> TaggedPeople { get; set; } = new List<PeopleTagViewModel>();

    public IObservableReadOnlyList<Rect> SuggestedFaces => suggestedFaces;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public Rect SelectionRect { get; set; }

    private readonly ISuggestionsService suggestionsService;

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    private readonly IFaceDetectionService faceDetectionService;

    private readonly IBitmapFileInfo bitmapFile;

    private readonly ObservableList<Rect> suggestedFaces = new ObservableList<Rect>();

    internal TagPeopleToolModel(
        IBitmapFileInfo bitmapFile,
        IMessenger messenger,
        ISuggestionsService suggestionsService,
        IMetadataService metadataService,
        IDialogService dialogService,
        IFaceDetectionService faceDetectionService) : base(messenger, false)
    {
        this.suggestionsService = suggestionsService;
        this.metadataService = metadataService;
        this.dialogService = dialogService;
        this.faceDetectionService = faceDetectionService;
        this.bitmapFile = bitmapFile;
    }

    public async Task InitializeAsync()
    {
        Messenger.Register<MetadataModifiedMessage>(this, Receive);
        Messenger.Register<TagPeopleToolActiveChangedMessage>(this, Receive);
        Messenger.Register<BitmapRotatedMesssage>(this, Receive);
        IsActive = Messenger.Send(new IsTagPeopleToolActiveRequestMessage());
        TaggedPeople = await LoadTaggedPeopleAsync();
    }

    private void Receive(TagPeopleToolActiveChangedMessage msg)
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

    private async void Receive(BitmapRotatedMesssage msg)
    {
        if (msg.Bitmap.Equals(bitmapFile))
        {
            TaggedPeople = await LoadTaggedPeopleAsync();
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
        Messenger.Send(new TagPeopleToolActiveChangedMessage(false));
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

    private async Task<List<PeopleTagViewModel>> LoadTaggedPeopleAsync()
    {
        return (await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People))
             .Where(peopleTag => !peopleTag.Rectangle.IsEmpty)
             .Select(peopleTag => new PeopleTagViewModel(IsActive, peopleTag.Name, peopleTag.Rectangle.ToRect()))
             .ToList();
    }

    private async Task ShowDetectedFacesAsync()
    {
        if (TaggedPeople.Any()) { return; }

        try
        {
            if (BitmapImage is ICanvasBitmapImageModel bitmapImage)
            {
                var detectedFaces = await faceDetectionService.DetectFacesAsync(bitmapImage, CancellationToken.None);

                if (!IsActive) { return; }

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

                if (IsEnabled)
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

    public override string ToString()
    {
        return nameof(TagPeopleToolModel) + "(" + bitmapFile.DisplayName + ")";
    }

}

