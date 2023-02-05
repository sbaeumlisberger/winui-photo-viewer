using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using Tocronx.SimpleAsync;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.Core.ViewModels
{
    public partial class SettingsPageModel : ViewModelBase
    {
        private readonly IDialogService dialogService;

        private readonly ISettingsService settingsService;

        private readonly SequentialTaskRunner saveSettingsTaskRunner = new SequentialTaskRunner();

        public IList<AppTheme> AvailableThemes { get; } = Enum.GetValues<AppTheme>();

        public IList<DeleteLinkedFilesOption> AvailableDeleteLinkedFilesOptions { get; } = Enum.GetValues<DeleteLinkedFilesOption>();

        public ApplicationSettings Settings { get; }

        public SettingsPageModel(
            IMessenger messenger,
            ApplicationSettings settings,
            ISettingsService settingsService,
            IDialogService dialogService) : base(messenger)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            Settings = settings;
        }

        protected override void OnViewConnectedOverride()
        {
            Settings.PropertyChanged += Settings_PropertyChanged;
        }

        protected override void OnViewDisconnectedOverride()
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
            Messenger.Send(new NavigateBackMessage());
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

        [RelayCommand]
        private async Task ShowLogAsync()
        {
            await Launcher.LaunchFileAsync(await Log.GetLogFileAsync());
        }

    }
}
