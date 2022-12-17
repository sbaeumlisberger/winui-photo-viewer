using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Messages;
using PhotoViewerCore.Utils;
using System.Linq;
using Tocronx.SimpleAsync;

namespace PhotoViewerCore.ViewModels;

public partial class KeywordsSectionModel : ViewModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> Keywords => keywords;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public IList<string> Suggestions { get; } = Array.Empty<string>();

    private ObservableList<ItemWithCountModel> keywords = new();

    private IList<IBitmapFileInfo> files = Array.Empty<IBitmapFileInfo>();

    private IList<MetadataView> metadata = Array.Empty<MetadataView>();

    private readonly SequentialTaskRunner writeFilesRunner;

    private readonly IMetadataService metadataService;

    public KeywordsSectionModel(
        SequentialTaskRunner writeFilesRunner,
        IMessenger messenger,
        IMetadataService metadataService) : base(messenger)
    {
        this.writeFilesRunner = writeFilesRunner;
        this.metadataService = metadataService;
        Messenger.Register<MetadataModifiedMessage>(this, OnReceive);
    }

    public void Update(IList<IBitmapFileInfo> files, IList<MetadataView> metadata)
    {
        this.files = files;
        this.metadata = metadata;
        keywords.Clear();
        keywords.AddRange(CreateListItemModels());
    }

    public void Clear()
    {
        files = Array.Empty<IBitmapFileInfo>();
        keywords.Clear();
    }

    partial void OnAutoSuggestBoxTextChanged()
    {
        UpdateSuggestions();
    }

    private void OnReceive(MetadataModifiedMessage msg)
    {
        if (msg.Files.Intersect(files).Any() && msg.MetadataProperty == MetadataProperties.Keywords)
        {
            RunOnUIThread(() =>
            {
                keywords.MatchTo(CreateListItemModels());

                if (keywords.FirstOrDefault(listItem => listItem.Value == AutoSuggestBoxText.Trim())
                        is ItemWithCountModel listItem && listItem.Count == listItem.Total)
                {
                    AutoSuggestBoxText = string.Empty;
                }
            });
        }
    }

    private IList<ItemWithCountModel> CreateListItemModels()
    {
        var values = metadata.Select(m => m.Get(MetadataProperties.Keywords)).ToList();
        return values.Flatten().GroupBy(x => x)
            .Select(group => new ItemWithCountModel(group.Key, group.Count(), values.Count)).ToList();
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
    private async Task AddKeywordAsync()
    {
        string keyword = AutoSuggestBoxText.Trim();
        await AddKeywordToFilesAsync(keyword, files.ToList());
    }

    [RelayCommand]
    private async Task RemoveKeywordAsync(string keyword)
    {
        await RemoveKeywordFromFilesAsync(keyword, files.ToList());
    }

    private async Task AddKeywordToFilesAsync(string keyword, IList<IBitmapFileInfo> files)
    {
        await writeFilesRunner.Enqueue(async () =>
        {
            var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
            {
                var keywordsOfFile = await metadataService.GetMetadataAsync(file, MetadataProperties.Keywords).ConfigureAwait(false);

                if (!keywordsOfFile.Contains(keyword))
                {
                    var keywords = keywordsOfFile.Append(keyword).ToArray();
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, keywords).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
            Messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, MetadataProperties.Keywords));
            // TODO show error message
        }).ConfigureAwait(false); ;
    }

    private async Task RemoveKeywordFromFilesAsync(string keyword, IList<IBitmapFileInfo> files)
    {
        await writeFilesRunner.Enqueue(async () =>
        {
            var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
            {
                var keywordsOfFile = await metadataService.GetMetadataAsync(file, MetadataProperties.Keywords).ConfigureAwait(false);

                if (keywordsOfFile.Contains(keyword))
                {
                    var keywords = keywordsOfFile.Except(Enumerable.Repeat(keyword, 1)).ToArray();
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, keywords).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
            Messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, MetadataProperties.Keywords));
            // TODO show error message
        }).ConfigureAwait(false); ;
    }
}
