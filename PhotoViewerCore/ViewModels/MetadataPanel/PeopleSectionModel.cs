using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Messages;
using PhotoViewerCore.Utils;
using Tocronx.SimpleAsync;

namespace PhotoViewerCore.ViewModels;

public partial class PeopleSectionModel : ViewModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> People => people;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public IList<string> Suggestions { get; } = Array.Empty<string>();

    public bool IsTagPeopleOnPhotoButtonVisible { get; }

    public bool IsTagPeopleOnPhotoButtonChecked { get; private set; }

    private ObservableList<ItemWithCountModel> people = new();

    private IList<IBitmapFileInfo> files = Array.Empty<IBitmapFileInfo>();

    private readonly SequentialTaskRunner writeFilesRunner;

    private readonly IMetadataService metadataService;

    public PeopleSectionModel(
        SequentialTaskRunner writeFilesRunner, 
        IMessenger messenger, 
        IMetadataService metadataService, 
        bool tagPeopleOnPhotoButtonVisible) : base(messenger)
    {
        this.writeFilesRunner = writeFilesRunner;
        this.metadataService = metadataService;
        IsTagPeopleOnPhotoButtonVisible = tagPeopleOnPhotoButtonVisible;       
    }

    protected override void OnViewConnectedOverride()
    {
        if (IsTagPeopleOnPhotoButtonVisible)
        {
            Messenger.Register<TagPeopleToolActiveChangedMeesage>(this, OnTagPeopleToolActiveChangedMessageReceived);
        }
        UpdateSuggestions();
    }

    public void Update(IList<IBitmapFileInfo> files, IList<MetadataView> metadata)
    {
        this.files = files;
        var values = metadata.Select(m => m.Get(MetadataProperties.People)).ToList();        
        people.Clear();
        people.AddRange(values.Flatten().GroupBy(peopleTag => peopleTag.Name.Trim())
            .Select(group => new ItemWithCountModel(group.Key, group.Count(), values.Count)));
    }

    public void Clear()
    {
        files = Array.Empty<IBitmapFileInfo>();
        people.Clear();
    }

    private void OnTagPeopleToolActiveChangedMessageReceived(TagPeopleToolActiveChangedMeesage msg)
    {
        IsTagPeopleOnPhotoButtonChecked = msg.IsVisible;
    }

    partial void OnAutoSuggestBoxTextChanged()
    {
        UpdateSuggestions();
    }

    private void UpdateSuggestions()
    {
        if (AutoSuggestBoxText == string.Empty)
        {
            // TODO show recent
        }
        else
        {
            // TODO find matches
        }
    }

    [RelayCommand]
    private async Task AddPersonAsync()
    {
        string personName = AutoSuggestBoxText.Trim();

        await writeFilesRunner.Enqueue(async () =>
        {
            foreach (var file in files)
            {
                var peopleTagsFromFile = await metadataService.GetMetadataAsync(file, MetadataProperties.People).ConfigureAwait(false);

                if (!peopleTagsFromFile.Any(peopleTag => peopleTag.Name == personName))
                {
                    peopleTagsFromFile.Add(new PeopleTag(personName));
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.People, peopleTagsFromFile).ConfigureAwait(false);
                }
            }
        });
        // TODO prallelize, handle errors

        var item = new ItemWithCountModel(personName, people.Count, people.Count);

        if (people.FirstOrDefault(item => item.Value == personName) is { } existingItem)
        {
            people[People.IndexOf(existingItem)] = item;
        }
        else
        {
            people.Add(item);
        }

        AutoSuggestBoxText = string.Empty;
    }

    [RelayCommand]
    private void ToggleTagPeopleOnPhoto()
    {
        Messenger.Send(new SetTagPeopleToolActive(!IsTagPeopleOnPhotoButtonChecked));
    }

}
