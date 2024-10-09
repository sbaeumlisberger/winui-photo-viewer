using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.ViewModels;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public interface IDeleteFilesService
{
    Task DeleteFilesAsync(IReadOnlyList<IMediaFileInfo> files);
}

public partial class DeleteFilesService : IDeleteFilesService
{
    private readonly IMessenger messenger;
    private readonly IDialogService dialogService;
    private readonly ISettingsService settingsService;
    private readonly IBackgroundTaskService backgroundTaskService;
    private readonly ApplicationSettings settings;

    internal DeleteFilesService(IMessenger messenger, IDialogService dialogService, ISettingsService settingsService, IBackgroundTaskService backgroundTaskService, ApplicationSettings settings)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.settingsService = settingsService;
        this.backgroundTaskService = backgroundTaskService;
        this.settings = settings;
    }

    public async Task DeleteFilesAsync(IReadOnlyList<IMediaFileInfo> files)
    {
        Log.Info($"Delete: {string.Join(", ", files.Select(file => file.DisplayName))}");

        bool deleteLinkedFiles = false;

        if (files.Any(file => file.LinkedStorageFiles.Any()))
        {
            deleteLinkedFiles = settings.DeleteLinkedFilesOption switch
            {
                DeleteLinkedFilesOption.Yes => true,
                DeleteLinkedFilesOption.No => false,
                _ => await ShowDeleteLinkedFilesDialog()
            };
        }

        var task = files.Parallel().TryProcessAsync(file => DeleteMediaFileAsync(file, deleteLinkedFiles));

        backgroundTaskService.RegisterBackgroundTask(task);

        var result = await task;

        messenger.Send(new MediaFilesDeletedMessage(result.ProcessedElements));

        if (result.HasFailures)
        {
            await ShowErrorDialogAsync(result);
        }
    }

    private async Task<bool> ShowDeleteLinkedFilesDialog()
    {
        var askDialogModel = new DeleteLinkedFilesDialogModel();

        await dialogService.ShowDialogAsync(askDialogModel);

        if (askDialogModel.IsRemember)
        {
            settings.DeleteLinkedFilesOption = askDialogModel.IsYes ? DeleteLinkedFilesOption.Yes : DeleteLinkedFilesOption.No;
            settingsService.SaveSettings(settings);
        }

        return askDialogModel.IsYes;
    }

    private async Task ShowErrorDialogAsync(ParallelResult<IMediaFileInfo> result)
    {
        string message = Strings.DeleteFilesErrorDialog_Message + "\n";
        message += string.Join("\n", result.Failures
            .Select(failure => failure.Element.DisplayName + " (" + failure.Exception.Message + ")"));

        var failuresDialog = new MessageDialogModel()
        {
            Title = Strings.DeleteFilesErrorDialog_Title,
            Message = message
        };

        await dialogService.ShowDialogAsync(failuresDialog);
    }

    private async Task DeleteMediaFileAsync(IMediaFileInfo media, bool deleteLinkedFiles)
    {
        if (deleteLinkedFiles)
        {
            foreach (var linkedFile in media.LinkedStorageFiles)
            {
                await DeleteStorageFileAsync(linkedFile).ConfigureAwait(false);
            }
        }

        await DeleteStorageFileAsync(media.StorageFile).ConfigureAwait(false);
    }

    private async Task DeleteStorageFileAsync(IStorageFile file)
    {
        try
        {
            await file.DeleteAsync().AsTask().ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            // files does no longer exist
        }
    }

}
