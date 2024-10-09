using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IImportGpxTrackCommand : IAcceleratedCommand { }

internal partial class ImportGpxTrackCommand : AsyncCommandBase, IImportGpxTrackCommand
{
    public VirtualKey AcceleratorKey => VirtualKey.G;

    public VirtualKeyModifiers AcceleratorModifiers => VirtualKeyModifiers.Control;

    private readonly ApplicationSession session;

    private readonly IMessenger messenger;

    private readonly IDialogService dialogService;

    private readonly IMetadataService metadataService;

    private readonly IGpxService gpxService;

    public ImportGpxTrackCommand(ApplicationSession session, IMessenger messenger, IDialogService dialogService, IMetadataService metadataService, IGpxService gpxService)
    {
        this.session = session;
        this.messenger = messenger;
        this.dialogService = dialogService;
        this.metadataService = metadataService;
        this.gpxService = gpxService;
    }

    protected override async Task OnExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new ImportGpxTrackDialogModel(messenger, dialogService, gpxService, session.Files, true));
    }
}
