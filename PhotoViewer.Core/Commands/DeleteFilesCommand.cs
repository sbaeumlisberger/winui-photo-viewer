using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Windows.Input;
using Windows.System;

namespace PhotoViewer.Core.Commands;

public interface IDeleteFilesCommand : IAsyncCommand<IReadOnlyCollection<IMediaFileInfo>>, IAcceleratedCommand, ICommand { }

public class DeleteFilesCommand : AsyncCommandBase<IReadOnlyCollection<IMediaFileInfo>>, IDeleteFilesCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.Delete;

    private readonly IMessenger messenger;
    private readonly IDeleteMediaFilesService deleteMediaFilesService;
    private readonly IDialogService dialogService;
    private readonly ISettingsService settingsService;
    private readonly IBackgroundTaskService backgroundTaskService;
    private readonly ApplicationSettings settings;

    internal DeleteFilesCommand(IMessenger messenger, IDeleteMediaFilesService deleteMediaService, IDialogService dialogService, ISettingsService settingsService, IBackgroundTaskService backgroundTaskService, ApplicationSettings settings)
    {
        this.messenger = messenger;
        this.deleteMediaFilesService = deleteMediaService;
        this.dialogService = dialogService;
        this.settingsService = settingsService;
        this.backgroundTaskService = backgroundTaskService;
        this.settings = settings;
    }

    protected override bool CanExecute(IReadOnlyCollection<IMediaFileInfo> parameter)
    {
        return parameter.Any();
    }

    protected override async Task OnExecuteAsync(IReadOnlyCollection<IMediaFileInfo> files)
    {
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

        var task = ParallelizationUtil.ProcessParallelAsync(files, async file =>
        {
            await deleteMediaFilesService.DeleteMediaFileAsync(file, deleteLinkedFiles);
        });

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

    private async Task ShowErrorDialogAsync(ProcessingResult<IMediaFileInfo> result)
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

}
