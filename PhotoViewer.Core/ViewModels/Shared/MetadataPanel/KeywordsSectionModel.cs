﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Concurrent;
using Tocronx.SimpleAsync;

namespace PhotoViewer.Core.ViewModels;

public partial class KeywordsSectionModel : MetadataPanelSectionModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> Keywords => keywords;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    private bool CanAddKeyword => !string.IsNullOrWhiteSpace(AutoSuggestBoxText);

    private ObservableList<ItemWithCountModel> keywords = new();

    private readonly IMetadataService metadataService;

    private readonly ISuggestionsService suggestionsService;

    private readonly IDialogService dialogService;

    internal KeywordsSectionModel(
        SequentialTaskRunner writeFilesRunner,
        IMessenger messenger,
        IMetadataService metadataService,
        ISuggestionsService suggestionsService,
        IDialogService dialogService) : base(writeFilesRunner, messenger)
    {
        this.metadataService = metadataService;
        this.suggestionsService = suggestionsService;
        this.dialogService = dialogService;
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

    public IReadOnlyList<string> FindSuggestions(string query)
    {
        return suggestionsService.FindMatches(query);
    }

    public IReadOnlyList<string> GetRecentSuggestions()
    {
        return suggestionsService.GetRecentSuggestions();
    }

    private List<ItemWithCountModel> CreateListItemModels(IList<MetadataView> metadata)
    {
        return metadata
            .Select(m => m.Get(MetadataProperties.Keywords))
            .Flatten()
            .GroupBy(keyword => keyword)
            .Select(group => new ItemWithCountModel(group.Key, group.Key.Split('/').Last(), group.Count(), metadata.Count))
            .ToList();
    }

    [RelayCommand(CanExecute = nameof(CanAddKeyword))]
    private async Task AddKeywordAsync()
    {
        string keyword = AutoSuggestBoxText.Trim();

        await EnqueueWriteFiles(async (files) =>
        {
            var result = await AddKeywordToFiles(files, keyword);

            if (result.IsSuccessful)
            {
                AutoSuggestBoxText = string.Empty;
                await suggestionsService.AddSuggestionAsync(keyword);
            }
            else
            {
                await ShowWriteMetadataFailedDialog(dialogService, result);
            }
        });
    }

    [RelayCommand]
    private async Task RemoveKeywordAsync(string keyword)
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
                await ShowWriteMetadataFailedDialog(dialogService, result);
            }
        });
    }

    [RelayCommand]
    private async Task ApplyKeywordToAllAsync(string keyword)
    {
        await EnqueueWriteFiles(async (files) =>
        {
            var result = await AddKeywordToFiles(files, keyword);

            if (result.HasFailures)
            {
                await ShowWriteMetadataFailedDialog(dialogService, result);
            }
        });
    }

    private async Task<ProcessingResult<IBitmapFileInfo>> AddKeywordToFiles(IReadOnlyCollection<IBitmapFileInfo> files, string keyword)
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

        return result;
    }

    [RelayCommand]
    private async Task ManageKeywordsSuggestionsAsync()
    {
        await dialogService.ShowDialogAsync(new ManageKeywordsDialogModel(suggestionsService, dialogService));
    }
}