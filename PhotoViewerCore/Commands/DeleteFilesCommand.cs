﻿using Microsoft.VisualBasic;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Models;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerCore.Commands;

public interface IDeleteFilesCommand
{
    Task ExecuteAsync(ICollection<IMediaFileInfo> files);
}

public class DeleteFilesCommand : IDeleteFilesCommand
{
    private readonly IMessenger messenger;
    private readonly IDeleteMediaService deleteMediaService;
    private readonly IDialogService dialogService;
    private readonly ISettingsService settingsService;
    private readonly ApplicationSettings settings;

    public DeleteFilesCommand(IMessenger messenger, IDeleteMediaService deleteMediaService, IDialogService dialogService, ISettingsService settingsService, ApplicationSettings settings)
    {
        this.messenger = messenger;
        this.deleteMediaService = deleteMediaService;
        this.dialogService = dialogService;
        this.settingsService = settingsService;
        this.settings = settings;
    }

    public async Task ExecuteAsync(ICollection<IMediaFileInfo> files)
    {
        bool deleteLinkedFiles = settings.DeleteLinkedFilesOption switch
        {
            DeleteLinkedFilesOption.Yes => true,
            DeleteLinkedFilesOption.No => false,
            _ => await ShowDeleteLinkedFilesDialog()
        };

        var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
        {
            await deleteMediaService.DeleteMediaAsync(file, deleteLinkedFiles);
        });

        messenger.Publish(new MediaItemsDeletedMessage(result.ProcessedElements));

        if (result.HasFailures)
        {
            await ShowFailuresDialogAsync(result);
        }
    }

    private async Task<bool> ShowDeleteLinkedFilesDialog()
    { 
        // TODO translate
        var askDialogModel = new YesNoDialogModel()
        {
            Title = "Verknüpfte Dateien löschen",
            Message = "Möchten Sie verknüpfte Dateien löschen?",
            RememberMessage = "Entscheidung merken (Kann in den Einstellungen geändert werden)"
        };

        await dialogService.ShowDialogAsync(askDialogModel);
       
        if (askDialogModel.IsRemember)
        {
            settings.DeleteLinkedFilesOption = askDialogModel.IsYes ? DeleteLinkedFilesOption.Yes : DeleteLinkedFilesOption.No;
            await settingsService.SaveSettingsAsync(settings);
        }

        return askDialogModel.IsYes;
    }

    private async Task ShowFailuresDialogAsync(ProcessingResult<IMediaFileInfo> result)
    {
        // TODO translate
        string message = "The following files could not be deleted:\n";
        message += string.Join("\n", result.Failures
            .Select(failure => failure.Element.Name + "(" + failure.Exception.Message + ")"));

        var failuresDialog = new MessageDialogModel()
        {
            Title = "Could not delete files",
            Message = message
        };

        await dialogService.ShowDialogAsync(failuresDialog);
    }

}
