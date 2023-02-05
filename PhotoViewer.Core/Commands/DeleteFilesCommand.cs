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

public interface IDeleteFilesCommand : IAcceleratedCommand, ICommand { }

public class DeleteFilesCommand : AsyncCommandBase<IReadOnlyCollection<IMediaFileInfo>>, IDeleteFilesCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.Delete;

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

    protected override bool CanExecute(IReadOnlyCollection<IMediaFileInfo> parameter)
    {
        return parameter.Any();
    }

    protected override async Task ExecuteAsync(IReadOnlyCollection<IMediaFileInfo> files)
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

        var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
        {
            await deleteMediaService.DeleteMediaAsync(file, deleteLinkedFiles);
        });

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
            await settingsService.SaveSettingsAsync(settings);
        }

        return askDialogModel.IsYes;
    }

    private async Task ShowErrorDialogAsync(ProcessingResult<IMediaFileInfo> result)
    {
        string message = Strings.DeleteFilesErrorDialog_Message + "\n";
        message += string.Join("\n", result.Failures
            .Select(failure => failure.Element.Name + "(" + failure.Exception.Message + ")"));

        var failuresDialog = new MessageDialogModel()
        {
            Title = Strings.DeleteFilesErrorDialog_Title,
            Message = message
        };

        await dialogService.ShowDialogAsync(failuresDialog);
    }

}
