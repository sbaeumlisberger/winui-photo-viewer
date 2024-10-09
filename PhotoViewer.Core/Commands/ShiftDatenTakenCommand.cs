using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IShiftDatenTakenCommand : IAcceleratedCommand { }

internal partial class ShiftDatenTakenCommand : AsyncCommandBase, IShiftDatenTakenCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.T;

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.Control;

    private readonly ApplicationSession session;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly IMetadataService metadataService;

    public ShiftDatenTakenCommand(ApplicationSession session, IMessenger messenger, IDialogService dialogService, IMetadataService metadataService)
    {
        this.session = session;
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.metadataService = metadataService;
    }

    protected override async Task OnExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new ShiftDatenTakenDialogModel(messenger, metadataService, session.Files));
    }
}
