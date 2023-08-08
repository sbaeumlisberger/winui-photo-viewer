using PhotoViewer.App.Services;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels.Dialogs;
using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IPrefixFilesByDateCommand : IAcceleratedCommand { }

internal class PrefixFilesByDateCommand : AsyncCommandBase, IPrefixFilesByDateCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.N;

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.Control;


    private readonly ApplicationSession session;

    private readonly ApplicationSettings settings;

    private readonly IDialogService dialogService;

    private readonly IMetadataService metadataService;

    public PrefixFilesByDateCommand(ApplicationSession session, ApplicationSettings settings, IDialogService dialogService, IMetadataService metadataService)
    {
        this.session = session;
        this.settings = settings;
        this.dialogService = dialogService;
        this.metadataService = metadataService;
    }

    protected override async Task OnExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new PrefixFilesByDateDialogModel(session.Files, settings, metadataService));
    }
}
