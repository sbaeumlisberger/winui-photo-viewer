﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using Windows.Foundation;

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

    public bool IsTagPeopleToolActive { get; private set; } = false;

    public bool IsSelectionEnabled => IsTagPeopleToolActive && IsEnabled;

    public bool IsUserSelecting { get; private set; } = false;

    public bool IsNameInputVisible => !IsUserSelecting && !SelectionRectInPercent.IsEmpty;

    public IBitmapImageModel? BitmapImage { get; set; }

    public float UIScaleFactor { get; set; } = 1;

    public IReadOnlyList<PeopleTagViewModel> TaggedPeople { get; set; } = new List<PeopleTagViewModel>();

    public IObservableReadOnlyList<Rect> SuggestedFaces => suggestedFacesInPercent;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public Rect SelectionRectInPercent { get; set; } = Rect.Empty;

    private readonly ISuggestionsService suggestionsService;

    private readonly IMetadataService metadataService;

    private readonly IDialogService dialogService;

    private readonly IFaceDetectionService faceDetectionService;

    private readonly IBitmapFileInfo bitmapFile;

    private readonly ObservableList<Rect> suggestedFacesInPercent = new ObservableList<Rect>();

    private List<Rect> detectedFaceRectsInPercent = new List<Rect>();

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
        IsTagPeopleToolActive = Messenger.Send(new IsTagPeopleToolActiveRequestMessage());
        await LoadTaggedPeopleAsync();
    }

    private void Receive(SetTagPeopleToolActiveMessage msg)
    {
        IsTagPeopleToolActive = msg.IsActive;
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
        AutoSuggestBoxText = string.Empty;
        SelectionRectInPercent = Rect.Empty;
        HideSuggestedFaces();

        if (msg.BitmapFile.Equals(bitmapFile))
        {
            await LoadTaggedPeopleAsync();
        }
    }

    async partial void OnBitmapImageChanged()
    {
        await DetectFacesAsync();

        if (IsSelectionEnabled)
        {
            ShowSuggestedFaces();
        }
    }

    partial void OnIsSelectionEnabledChanged()
    {
        if (IsSelectionEnabled)
        {
            TaggedPeople.ForEach(x => x.IsVisible = true);
            ShowSuggestedFaces();
        }
        else
        {
            TaggedPeople.ForEach(x => x.IsVisible = false);
            AutoSuggestBoxText = string.Empty;
            SelectionRectInPercent = Rect.Empty;
            HideSuggestedFaces();
        }
    }

    private void HideSuggestedFaces()
    {
        suggestedFacesInPercent.Clear();
    }

    private void ShowSuggestedFaces()
    {
        if (TaggedPeople.IsEmpty())
        {
            suggestedFacesInPercent.MatchTo(detectedFaceRectsInPercent);
        }
        TrySelectNextDetectedFace();
    }

    public void ExitPeopleTagging()
    {
        Messenger.Send(new SetTagPeopleToolActiveMessage(false));
    }

    public IReadOnlyList<string> FindSuggestions(string text)
    {
        return suggestionsService.FindMatches(text, exclude: TaggedPeople.Select(x => x.Name).ToList());
    }

    public IReadOnlyList<string> GetRecentSuggestions()
    {
        return suggestionsService.GetRecent(exclude: TaggedPeople.Select(x => x.Name).ToList());
    }

    public void TrySelectNextDetectedFace()
    {
        if (suggestedFacesInPercent.Any())
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRectInPercent = suggestedFacesInPercent.First();
            suggestedFacesInPercent.RemoveAt(0);
        }
        else
        {
            AutoSuggestBoxText = string.Empty;
            SelectionRectInPercent = Rect.Empty;
        }
    }

    public void OnUserStartedSelection()
    {
        IsUserSelecting = true;
        suggestedFacesInPercent.Clear();
    }

    public void OnUserEndedSelection()
    {
        IsUserSelecting = false;
    }

    [RelayCommand]
    private async Task AddPersonAsync()
    {
        string personName = AutoSuggestBoxText.Trim();

        if (personName == string.Empty)
        {
            return;
        }

        var orientation = await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.Orientation);

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

        var faceRect = ReverseRotateRect(SelectionRectInPercent, orientation);

        people.Add(new PeopleTag(personName, new FaceRect(faceRect.X, faceRect.Y, faceRect.Width, faceRect.Height)));

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
        var orientation = await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.Orientation);
        TaggedPeople = (await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.People))
             .Where(peopleTag => peopleTag.Rectangle is not null)
             .Select(peopleTag => new PeopleTagViewModel(IsSelectionEnabled, peopleTag.Name, RotateRect(peopleTag.Rectangle!.Value.ToRect(), orientation)))
             .ToList();
    }

    private Rect ReverseRotateRect(Rect rect, PhotoOrientation orientation)
    {
        switch (orientation)
        {
            case PhotoOrientation.Normal:
            case PhotoOrientation.Unspecified:
                return rect;
            case PhotoOrientation.Rotate90:
                return new Rect(1 - rect.Bottom, rect.X, rect.Height, rect.Width);
            case PhotoOrientation.Rotate180:
                return new Rect(1 - rect.Right, 1 - rect.Bottom, rect.Width, rect.Height);
            case PhotoOrientation.Rotate270:
                return new Rect(rect.Y, 1 - rect.Right, rect.Height, rect.Width);
            default:
                throw new NotSupportedException("Unsupported orientation: " + orientation);
        }
    }

    private Rect RotateRect(Rect rect, PhotoOrientation orientation)
    {
        switch (orientation)
        {
            case PhotoOrientation.Normal:
            case PhotoOrientation.Unspecified:
                return rect;
            case PhotoOrientation.Rotate90:
                return new Rect(rect.Y, 1 - rect.Right, rect.Height, rect.Width);
            case PhotoOrientation.Rotate180:
                return new Rect(1 - rect.Right, 1 - rect.Bottom, rect.Width, rect.Height);
            case PhotoOrientation.Rotate270:
                return new Rect(1 - rect.Bottom, rect.X, rect.Height, rect.Width);
            default:
                throw new NotSupportedException("Unsupported orientation: " + orientation);
        }
    }

    private async Task DetectFacesAsync()
    {
        if (TaggedPeople.Any())
        {
            return;
        }

        try
        {
            if (BitmapImage is ICanvasBitmapImageModel bitmapImage)
            {
                var detectedFaces = await faceDetectionService.DetectFacesAsync(bitmapImage);

                var detectedFaceRectsInPercent = new List<Rect>();

                var imageSize = bitmapImage.SizeInDIPs;

                foreach (var face in detectedFaces)
                {
                    var faceBox = face.FaceBox;

                    detectedFaceRectsInPercent.Add(new Rect(
                        faceBox.X / imageSize.Width,
                        faceBox.Y / imageSize.Height,
                        faceBox.Width / imageSize.Width,
                        faceBox.Height / imageSize.Height));
                }

                this.detectedFaceRectsInPercent = detectedFaceRectsInPercent;
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

