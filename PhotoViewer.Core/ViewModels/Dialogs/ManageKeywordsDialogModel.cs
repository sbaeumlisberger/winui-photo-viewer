using CommunityToolkit.Mvvm.Input;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Services;

namespace PhotoViewer.Core.ViewModels;

public partial class ManageKeywordsDialogModel : ViewModelBase
{
    public IReadOnlyList<string> Keywords { get; set; } = new List<string>();

    private readonly ISuggestionsService suggestionsService;
    private readonly IDialogService dialogService;

    internal ManageKeywordsDialogModel(ISuggestionsService suggestionsService, IDialogService dialogService)
    {
        this.suggestionsService = suggestionsService;
        this.dialogService = dialogService;

        Keywords = suggestionsService.GetAll();
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

        Keywords = suggestionsService.GetAll();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePickerModel = new FileSavePickerModel()
        {
            SuggestedFileName = "keywords.json",
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
        Keywords = suggestionsService.GetAll();
    }

    [RelayCommand]
    private async Task RemoveAsync(string keyword)
    {
        await suggestionsService.RemoveSuggestionAsync(keyword);
        Keywords = suggestionsService.GetAll();
    }
}
