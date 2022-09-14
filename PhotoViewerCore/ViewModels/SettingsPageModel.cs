using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.ViewModels;
using PhotoViewerCore.Models;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using System.ComponentModel;
using Tocronx.SimpleAsync;
using Windows.Storage;

namespace PhotoViewerCore.ViewModels
{
    public partial class SettingsPageModel : ViewModelBase
    {
        private readonly IMessenger messenger;

        private readonly IDialogService dialogService;

        private readonly ISettingsService settingsService;

        private readonly SequentialTaskRunner saveSettingsTaskRunner = new SequentialTaskRunner();

        public IList<DeleteLinkedFilesOption> AvailableDeleteLinkedFilesOptions { get; } = Enum.GetValues<DeleteLinkedFilesOption>();

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

        public void OnViewLoaded()
        {
            Settings.PropertyChanged += Settings_PropertyChanged;
        }

        public void OnViewUnloaded() 
        {
            Settings.PropertyChanged -= Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            saveSettingsTaskRunner.EnqueueIfEmpty(() => settingsService.SaveSettingsAsync(Settings));
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
                var importedSettings = await settingsService.ImportSettingsAsync(file);
                ApplicationSettingsProvider.SetSettings(importedSettings);
            }
        }


        [RelayCommand]
        private async Task ResetAsync()
        {
            await settingsService.SaveSettingsAsync(new ApplicationSettings());
        }

    }
}
