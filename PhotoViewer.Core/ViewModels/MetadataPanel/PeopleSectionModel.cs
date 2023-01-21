using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Messages;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using System.Collections.Concurrent;
using Tocronx.SimpleAsync;
using Windows.Foundation.Collections;

namespace PhotoViewerCore.ViewModels;

public partial class PeopleSectionModel : MetadataPanelSectionModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> People => people;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public bool IsTagPeopleOnPhotoButtonVisible { get; }

    public bool IsTagPeopleOnPhotoButtonChecked { get; private set; }

    private ObservableList<ItemWithCountModel> people = new();

    private readonly IMetadataService metadataService;

    private readonly ISuggestionsService suggestionsService;

    internal PeopleSectionModel(
        SequentialTaskRunner writeFilesRunner,
        IMessenger messenger,
        IMetadataService metadataService,
        ISuggestionsService suggestionsService,
        bool tagPeopleOnPhotoButtonVisible) : base(writeFilesRunner, messenger)
    {
        this.metadataService = metadataService;
        this.suggestionsService = suggestionsService;
        IsTagPeopleOnPhotoButtonVisible = tagPeopleOnPhotoButtonVisible;

        if (IsTagPeopleOnPhotoButtonVisible)
        {
            Messenger.Register<TagPeopleToolActiveChangedMeesage>(this, OnTagPeopleToolActiveChangedMessageReceived);
        }
    }

    protected override void OnFilesChanged(IList<MetadataView> metadata)
    {
        people.Clear();
        people.AddRange(CreateListItemModels(metadata));
    }

    protected override void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        people.MatchTo(CreateListItemModels(metadata));
    }

    private void OnTagPeopleToolActiveChangedMessageReceived(TagPeopleToolActiveChangedMeesage msg)
    {
        IsTagPeopleOnPhotoButtonChecked = msg.IsActive;
    }

    public IReadOnlyList<string> FindSuggestions(string text)
    {
        return suggestionsService.FindMatches(text);
    }

    public IReadOnlyList<string> GetRecentSuggestions()
    {
        return suggestionsService.GetRecentSuggestions();
    }

    private List<ItemWithCountModel> CreateListItemModels(IList<MetadataView> metadata)
    {
        return metadata
            .Select(m => m.Get(MetadataProperties.People))
            .Flatten()
            .GroupBy(peopleTag => peopleTag.Name.Trim())
            .Select(group => new ItemWithCountModel(group.Key, group.Count(), metadata.Count))
            .ToList();
    }


    [RelayCommand]
    private async Task AddPersonAsync()
    {
        string personName = AutoSuggestBoxText.Trim();

        await EnqueueWriteFiles(async (files) =>
        {
            var modifiedFiles = new ConcurrentBag<IBitmapFileInfo>();

            var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
            {
                var peopleTagsFromFile = await metadataService.GetMetadataAsync(file, MetadataProperties.People).ConfigureAwait(false);

                if (!peopleTagsFromFile.Any(peopleTag => peopleTag.Name == personName))
                {
                    peopleTagsFromFile.Add(new PeopleTag(personName));
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.People, peopleTagsFromFile).ConfigureAwait(false);
                    modifiedFiles.Add(file);
                }
            });

            Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.People));

            if (result.IsSuccessful)
            {
                AutoSuggestBoxText = string.Empty;
                await suggestionsService.AddSuggestionAsync(personName);
            }
            else
            {
                // TODO show error message
            }
        });
    }

    [RelayCommand]
    private void ToggleTagPeopleOnPhoto()
    {
        Messenger.Send(new TagPeopleToolActiveChangedMeesage(!IsTagPeopleOnPhotoButtonChecked));
    }

}
