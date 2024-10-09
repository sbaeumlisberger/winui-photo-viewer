using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IPrefixFilesByDateCommand : IAcceleratedCommand { }

internal partial class PrefixFilesByDateCommand : AsyncCommandBase, IPrefixFilesByDateCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.S;

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.Control;


    private readonly ApplicationSession session;

    private readonly IDialogService dialogService;

    private readonly IMetadataService metadataService;

    private readonly IMessenger messenger;

    public PrefixFilesByDateCommand(ApplicationSession session, IDialogService dialogService, IMetadataService metadataService, IMessenger messenger)
    {
        this.session = session;
        this.dialogService = dialogService;
        this.metadataService = metadataService;
        this.messenger = messenger;
    }

    protected override async Task OnExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new PrefixFilesByDateDialogModel(session.Files, metadataService, messenger));
    }
}
