using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IDeleteSingleRawFilesCommand : IAcceleratedCommand { }

internal partial class DeleteSingleRawFilesCommand : AsyncCommandBase, IDeleteSingleRawFilesCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.W;

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.Control;

    private readonly ApplicationSession session;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    public DeleteSingleRawFilesCommand(ApplicationSession session, IMessenger messenger, IDialogService dialogService)
    {
        this.session = session;
        this.messenger = messenger;
        this.dialogService = dialogService;
    }

    protected override async Task OnExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new DeleteSingleRawFilesDialogModel(messenger, session.Files));
    }
}
