using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IMoveRawFilesToSubfolderCommand : IAcceleratedCommand { }

internal partial class MoveRawFilesToSubfolderCommand : AsyncCommandBase, IMoveRawFilesToSubfolderCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.U;

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.Control;

    private readonly ApplicationSession session;

    private readonly ApplicationSettings settings;

    private readonly IDialogService dialogService;

    public MoveRawFilesToSubfolderCommand(ApplicationSession session, ApplicationSettings settings, IDialogService dialogService)
    {
        this.session = session;
        this.settings = settings;
        this.dialogService = dialogService;
    }

    protected override async Task OnExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new MoveRawFilesToSubfolderDialogModel(session.Files, settings));
    }
}
