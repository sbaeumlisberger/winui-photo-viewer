using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Utils;

namespace PhotoViewerCore.ViewModels;

public partial class KeywordsSectionModel : ViewModelBase
{
    public IObservableReadOnlyList<ItemWithCountModel> Keywords => keywords;

    public string AutoSuggestBoxText { get; set; } = string.Empty;

    public IList<string> Suggestions { get; } = Array.Empty<string>();

    private ObservableList<ItemWithCountModel> keywords = new();

    private IList<IBitmapFileInfo> files = Array.Empty<IBitmapFileInfo>();

    private readonly IMetadataService metadataService;

    public KeywordsSectionModel(IMetadataService metadataService)
    {
        this.metadataService = metadataService;
        UpdateSuggestions();
    }

    public void SetValues(IList<IBitmapFileInfo> files, IList<string[]> values)
    {
        this.files = files;
        keywords.Clear();
        keywords.AddRange(values.Flatten().GroupBy(x => x)
            .Select(group => new ItemWithCountModel(group.Key, group.Count(), values.Count)));
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

        foreach (var file in files)
        {
            var keywordsOfFile = await metadataService.GetMetadataAsync(file, MetadataProperties.Keywords);

            if (!keywordsOfFile.Contains(keyword))
            {
                await metadataService.WriteMetadataAsync(file, MetadataProperties.Keywords, keywordsOfFile.Append(keyword).ToArray());
            }
        }

        var item = new ItemWithCountModel(keyword, keywords.Count, keywords.Count);

        if (keywords.FirstOrDefault(item => item.Value == keyword) is { } existingItem)
        {
            keywords[Keywords.IndexOf(existingItem)] = item;
        }
        else
        {
            keywords.Add(item);
        }

        AutoSuggestBoxText = string.Empty;
    }

}
