using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataAPI.Data;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Concurrent;
using Tocronx.SimpleAsync;
using Windows.Foundation.Collections;

namespace PhotoViewer.Core.ViewModels;

public partial class PeopleSectionModel : MetadataPanelSectionModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> People => people;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public bool IsTagPeopleOnPhotoButtonVisible { get; }

    public bool IsTagPeopleOnPhotoButtonChecked { get; private set; }

    private ObservableList<ItemWithCountModel> people = new();

    private readonly IMetadataService metadataService;

    private readonly ISuggestionsService suggestionsService;

    private readonly IDialogService dialogService;

    internal PeopleSectionModel(
        SequentialTaskRunner writeFilesRunner,
        IMessenger messenger,
        IMetadataService metadataService,
        ISuggestionsService suggestionsService,
        IDialogService dialogService,
        bool tagPeopleOnPhotoButtonVisible) : base(writeFilesRunner, messenger)
    {
        this.metadataService = metadataService;
        this.suggestionsService = suggestionsService;
        this.dialogService = dialogService;
        IsTagPeopleOnPhotoButtonVisible = tagPeopleOnPhotoButtonVisible;

        if (IsTagPeopleOnPhotoButtonVisible)
        {
            Messenger.Register<TagPeopleToolActiveChangedMessage>(this, Receive);
            Messenger.Register<IsTagPeopleToolActiveRequestMessage>(this, Receive);
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

    private void Receive(TagPeopleToolActiveChangedMessage msg)
    {
        IsTagPeopleOnPhotoButtonChecked = msg.IsActive;
    }

    private void Receive(IsTagPeopleToolActiveRequestMessage msg)
    {
        msg.Reply(IsTagPeopleOnPhotoButtonChecked);
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
                await ShowWriteMetadataFailedDialog(dialogService, result);
            }
        });
    }

    [RelayCommand]
    private async Task RemovePeopleTagAsync(string personName)
    {
        await EnqueueWriteFiles(async (files) =>
        {
            var modifiedFiles = new ConcurrentBag<IBitmapFileInfo>();

            var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
            {
                var peopleTags = await metadataService.GetMetadataAsync(file, MetadataProperties.People).ConfigureAwait(false);

                if (peopleTags.RemoveFirst(peopleTag => peopleTag.Name == personName))
                {
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.People, peopleTags).ConfigureAwait(false);
                    modifiedFiles.Add(file);
                }
            });

            Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.People));

            if (result.HasFailures)
            {
                await ShowWriteMetadataFailedDialog(dialogService, result);
            }
        });
    }

    [RelayCommand]
    private void ToggleTagPeopleOnPhoto()
    {
        Messenger.Send(new TagPeopleToolActiveChangedMessage(!IsTagPeopleOnPhotoButtonChecked));
    }

}
