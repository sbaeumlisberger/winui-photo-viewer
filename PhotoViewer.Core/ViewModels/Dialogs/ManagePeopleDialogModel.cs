using CommunityToolkit.Mvvm.Input;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class ManagePeopleDialogModel : ViewModelBase
{
    public IReadOnlyList<string> PeopleNames { get; set; } = new List<string>();

    public string SearchText { get; set; } = string.Empty;

    private readonly ISuggestionsService suggestionsService;
    private readonly IDialogService dialogService;

    internal ManagePeopleDialogModel(ISuggestionsService suggestionsService, IDialogService dialogService)
    {
        this.suggestionsService = suggestionsService;
        this.dialogService = dialogService;

        PeopleNames = suggestionsService.GetAll();
    }

    partial void OnSearchTextChanged()
    {
        PeopleNames = suggestionsService.GetAll(SearchText);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var filePickerModel = new FileOpenPickerModel()
        {
            FileTypeFilter = new[] { ".json" }
        };

        await dialogService.ShowDialogAsync(filePickerModel);

        if (filePickerModel.File is { } file)
        {
            await suggestionsService.ImportFromFileAsync(file);
        }

        PeopleNames = suggestionsService.GetAll();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePickerModel = new FileSavePickerModel()
        {
            SuggestedFileName = "people.json",
            FileTypeChoices = new() { { "JSON", new[] { ".json" } } }
        };

        await dialogService.ShowDialogAsync(filePickerModel);

        if (filePickerModel.File is { } file)
        {
            await suggestionsService.ExportToFileAsync(file);
        }
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        await suggestionsService.ResetAsync();
        PeopleNames = suggestionsService.GetAll();
    }

    [RelayCommand]
    private async Task RemoveAsync(string keyword)
    {
        await suggestionsService.RemoveSuggestionAsync(keyword);
        PeopleNames = suggestionsService.GetAll();
    }
}
