using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using System.Collections.Concurrent;

namespace PhotoViewer.Core.ViewModels;

public partial class KeywordsSectionModel : MetadataPanelSectionModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> Keywords => keywords;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    private bool CanAddKeyword => !string.IsNullOrWhiteSpace(AutoSuggestBoxText) && !IsWriting;

    private readonly ObservableList<ItemWithCountModel> keywords = new();

    private readonly IMetadataService metadataService;

    private readonly ISuggestionsService suggestionsService;

    private readonly IDialogService dialogService;

    internal KeywordsSectionModel(
        IMessenger messenger,
        IMetadataService metadataService,
        ISuggestionsService suggestionsService,
        IDialogService dialogService,
        IBackgroundTaskService backgroundTaskService) : base(messenger, backgroundTaskService, dialogService)
    {
        this.metadataService = metadataService;
        this.suggestionsService = suggestionsService;
        this.dialogService = dialogService;
    }

    protected override void OnFilesChanged(IReadOnlyList<MetadataView> metadata)
    {
        keywords.Clear();
        keywords.AddRange(CreateListItemModels(metadata));
        AutoSuggestBoxText = string.Empty;
    }

    protected override void OnMetadataModified(IReadOnlyList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        if (metadataProperty == MetadataProperties.Keywords)
        {
            keywords.MatchTo(CreateListItemModels(metadata));
        }
    }

    partial void OnIsWritingChanged()
    {
        OnPropertyChanged(nameof(CanAddKeyword));
    }

    public IReadOnlyList<string> FindSuggestions(string query)
    {
        return suggestionsService.FindMatches(query);
    }

    private List<ItemWithCountModel> CreateListItemModels(IReadOnlyList<MetadataView> metadata)
    {
        return metadata
            .Select(m => m.Get(MetadataProperties.Keywords))
            .Flatten()
            .GroupBy(keyword => keyword)
            .Select(group => new ItemWithCountModel(group.Key, group.Key.Split('/').Last(), group.Count(), metadata.Count))
            .ToList();
    }

    [RelayCommand(CanExecute = nameof(CanAddKeyword), AllowConcurrentExecutions = true)]
    private async Task AddKeywordAsync()
    {
        string keyword = AutoSuggestBoxText.Trim();

        if (await AddKeywordToFiles(keyword))
        {
            if (AutoSuggestBoxText.Trim() == keyword)
            {
                AutoSuggestBoxText = string.Empty;
            }
            await suggestionsService.AddSuggestionAsync(keyword);
        }
    }

    [RelayCommand]
    private async Task RemoveKeywordAsync(string keyword)
    {
        var modifiedFiles = new ConcurrentBag<IBitmapFileInfo>();

        await WriteFilesAsync(async file =>
        {
            var keywordsOfFile = await metadataService.GetMetadataAsync(file, MetadataProperties.Keywords).ConfigureAwait(false);

            if (keywordsOfFile.Contains(keyword))
            {
                var keywords = keywordsOfFile.Except(Enumerable.Repeat(keyword, 1)).ToArray();
                await metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, keywords).ConfigureAwait(false);
                modifiedFiles.Add(file);
            }
        },
        _ => Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.Keywords)));
    }

    [RelayCommand]
    private async Task ApplyKeywordToAllAsync(string keyword)
    {
        await AddKeywordToFiles(keyword);
    }

    private async Task<bool> AddKeywordToFiles(string keyword)
    {
        var modifiedFiles = new ConcurrentBag<IBitmapFileInfo>();

        return await WriteFilesAsync(async file =>
        {
            var keywordsOfFile = await metadataService.GetMetadataAsync(file, MetadataProperties.Keywords).ConfigureAwait(false);

            if (!keywordsOfFile.Contains(keyword))
            {
                var keywords = keywordsOfFile.Append(keyword).ToArray();
                await metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, keywords).ConfigureAwait(false);
                modifiedFiles.Add(file);
            }
        },
        _ => Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.Keywords)));
    }

    [RelayCommand]
    private async Task ManageKeywordsSuggestionsAsync()
    {
        await dialogService.ShowDialogAsync(new ManageKeywordsDialogModel(suggestionsService, dialogService));
    }
}
