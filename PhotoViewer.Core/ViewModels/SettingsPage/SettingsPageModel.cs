using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.Core.ViewModels
{
    public partial class SettingsPageModel : ViewModelBase
    {
        private readonly IDialogService dialogService;

        private readonly ISettingsService settingsService;

        public IList<AppTheme> AvailableThemes { get; } = Enum.GetValues<AppTheme>();

        public IList<DeleteLinkedFilesOption> AvailableDeleteLinkedFilesOptions { get; } = Enum.GetValues<DeleteLinkedFilesOption>();

        public ApplicationSettings Settings { get; }

        public string AppName { get; }

        public string Version { get; }

        internal SettingsPageModel(
            IMessenger messenger,
            ApplicationSettings settings,
            ISettingsService settingsService,
            IDialogService dialogService) : base(messenger)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            Settings = settings;
            Settings.PropertyChanged += Settings_PropertyChanged;
            AppName = AppData.ApplicationName;
            var version = AppData.Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}";
        }

        protected override void OnCleanup()
        {
            Settings.PropertyChanged -= Settings_PropertyChanged;
        }

        public void OnNavigatedTo()
        {
            Messenger.Send(new ChangeWindowTitleMessage(Strings.SettingsPage_Title));
        }

        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Settings.DiashowTime == TimeSpan.Zero)
            {
                Settings.DiashowTime = ApplicationSettings.DefaultDiashowTime;
            }
            settingsService.SaveSettings(Settings);
            Messenger.Send(new SettingsChangedMessage(e.PropertyName));
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
                SuggestedFileName = "settings.ini",
                FileTypeChoices = new() { { "INI", new[] { ".ini" } } }
            };

            await dialogService.ShowDialogAsync(fileSavePickerModel);

            if (fileSavePickerModel.File is IStorageFile file)
            {
                settingsService.ExportSettings(file);
            }
        }

        [RelayCommand]
        private async Task ImportAsync()
        {
            var fileOpenPickerModel = new FileOpenPickerModel()
            {
                FileTypeFilter = new[] { ".ini" }
            };

            await dialogService.ShowDialogAsync(fileOpenPickerModel);

            if (fileOpenPickerModel.File is IStorageFile file)
            {
                try
                {
                    var importedSettings = settingsService.ImportSettings(file);
                    Settings.Apply(importedSettings);
                    Messenger.Send(new SettingsChangedMessage(null));
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to import settings", ex);
                    await dialogService.ShowDialogAsync(new MessageDialogModel()
                    {
                        Title = Strings.ImportSettingsFailedDialog_Title,
                        Message = Strings.ImportSettingsFailedDialog_Message
                    });
                }
            }
        }


        [RelayCommand]
        private void Reset()
        {
            Settings.Apply(new ApplicationSettings());
            settingsService.SaveSettings(Settings);
            Messenger.Send(new SettingsChangedMessage(null));
        }

        [RelayCommand]
        private async Task ShowLogAsync()
        {
            var logFile = await StorageFile.GetFileFromPathAsync(Log.Logger.Appenders.OfType<FileAppender>().First().LogFilePath);
            await Launcher.LaunchFileAsync(logFile);
        }

    }
}
