using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels.Dialogs;

using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IDeleteSingleRawFilesCommand : IAcceleratedCommand { }

internal class DeleteSingleRawFilesCommand : AsyncCommandBase, IDeleteSingleRawFilesCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.W;

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.Control;

    private readonly ApplicationSession session;

    private readonly IDialogService dialogService;

    public DeleteSingleRawFilesCommand(ApplicationSession session, IDialogService dialogService) 
    {
        this.session = session;
        this.dialogService = dialogService;
    }

    protected override async Task OnExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new DeleteSingleRawFilesDialogModel(session.Files));
    }
}
