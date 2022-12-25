using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Messages;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using System.Collections.Concurrent;
using Tocronx.SimpleAsync;

namespace PhotoViewerCore.ViewModels;

public partial class KeywordsSectionModel : MetadataPanelSectionModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> Keywords => keywords;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public IReadOnlyList<string> Suggestions { get; private set; } = Array.Empty<string>();

    private ObservableList<ItemWithCountModel> keywords = new();

    private readonly IMetadataService metadataService;

    private readonly ISuggestionsService suggestionsService;

    internal KeywordsSectionModel(
        SequentialTaskRunner writeFilesRunner, 
        IMessenger messenger, 
        IMetadataService metadataService,
        ISuggestionsService suggestionsService) : base(writeFilesRunner, messenger)
    {
        this.metadataService = metadataService;
        this.suggestionsService = suggestionsService;
    }

    protected override void OnFilesChanged(IList<MetadataView> metadata)
    {
        keywords.Clear();
        keywords.AddRange(CreateListItemModels(metadata));
    }

    protected override void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty)
    {
        if (metadataProperty == MetadataProperties.Keywords)
        {
            keywords.MatchTo(CreateListItemModels(metadata));
        }
    }

    partial void OnAutoSuggestBoxTextChanged()
    {
        UpdateSuggestions();
    }

    public void OnAutoSuggestBoxFocused()
    {
        UpdateSuggestions();
    }

    private List<ItemWithCountModel> CreateListItemModels(IList<MetadataView> metadata)
    {
        return metadata
            .Select(m => m.Get(MetadataProperties.Keywords))
            .Flatten()
            .GroupBy(keyword => keyword)
            .Select(group => new ItemWithCountModel(group.Key, group.Count(), metadata.Count))
            .ToList();
    }


    private void UpdateSuggestions()
    {
        if (AutoSuggestBoxText == string.Empty)
        {
            Suggestions = suggestionsService.GetRecentSuggestions();
        }
        else
        {
            Suggestions = suggestionsService.FindMatches(AutoSuggestBoxText);
        }
    }

    [RelayCommand]
    private async Task AddKeywordAsync()
    {
        string keyword = AutoSuggestBoxText.Trim();
        await AddKeywordToFilesAsync(keyword);
    }

    [RelayCommand]
    private async Task RemoveKeywordAsync(string keyword)
    {
        await RemoveKeywordFromFilesAsync(keyword);
    }

    private async Task AddKeywordToFilesAsync(string keyword)
    {
        await EnqueueWriteFiles(async (files) =>
        {
            var modifiedFiles = new ConcurrentBag<IBitmapFileInfo>();

            var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
            {
                var keywordsOfFile = await metadataService.GetMetadataAsync(file, MetadataProperties.Keywords).ConfigureAwait(false);

                if (!keywordsOfFile.Contains(keyword))
                {
                    var keywords = keywordsOfFile.Append(keyword).ToArray();
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, keywords).ConfigureAwait(false);
                    modifiedFiles.Add(file);
                }
            });

            Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.Keywords));

            if (result.IsSuccessful)
            {
                AutoSuggestBoxText = string.Empty;
                await suggestionsService.AddSuggestionAsync(keyword);
            }
            else 
            {
                // TODO show error message
            }
        });
    }

    private async Task RemoveKeywordFromFilesAsync(string keyword)
    {
        await EnqueueWriteFiles(async (files) =>
        {
            var modifiedFiles = new ConcurrentBag<IBitmapFileInfo>();

            var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
            {
                var keywordsOfFile = await metadataService.GetMetadataAsync(file, MetadataProperties.Keywords).ConfigureAwait(false);

                if (keywordsOfFile.Contains(keyword))
                {
                    var keywords = keywordsOfFile.Except(Enumerable.Repeat(keyword, 1)).ToArray();
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, keywords).ConfigureAwait(false);
                    modifiedFiles.Add(file);
                }
            });

            Messenger.Send(new MetadataModifiedMessage(modifiedFiles, MetadataProperties.Keywords));
            
            if (result.HasFailures)
            {
                // TODO show error message
            }
        });
    }

}
