using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.Models;
using PhotoViewerCore.Services;
using Windows.Storage;

namespace PhotoViewerCore.ViewModels
{
    public partial class SettingsPageModel : ViewModelBase
    {
        private readonly IMessenger messenger;

        private readonly IDialogService dialogService;

        private readonly ISettingsService settingsService;

        public ApplicationSettings Settings { get; }

        public SettingsPageModel(
            IMessenger messenger,
            ApplicationSettings settings,
            ISettingsService settingsService,
            IDialogService dialogService)
        {
            this.messenger = messenger;
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            Settings = settings;
        }

        [RelayCommand]
        private void NavigateBack()
        {
            messenger.Publish(new NavigateBackMessage());
        }

        [RelayCommand]
        private async Task ExportAsync()
        {
            var fileSavePickerModel = new FileSavePickerModel()
            {
                SuggestedFileName = "settings.json",
                FileTypeChoices = new Dictionary<string, IList<string>>() { { "JSON", new[] { ".json" } } }
            };

            await dialogService.ShowDialogAsync(fileSavePickerModel);

            if (fileSavePickerModel.File is IStorageFile file)
            {
                await settingsService.ExportSettingsAsync(file);
            }
        }

        [RelayCommand]
        private async Task ImportAsync()
        {
            var fileOpenPickerModel = new FileOpenPickerModel()
            {
                FileTypeFilter = new[] { ".json" }
            };

            await dialogService.ShowDialogAsync(fileOpenPickerModel);

            if (fileOpenPickerModel.File is IStorageFile file)
            {
                await settingsService.ImportSettingsAsync(file);
            }
        }


        [RelayCommand]
        private async Task ResetAsync()
        {
            await settingsService.SaveSettingsAsync(new ApplicationSettings());
        }

    }
}
