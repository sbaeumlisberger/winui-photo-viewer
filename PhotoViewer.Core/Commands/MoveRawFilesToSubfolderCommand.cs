﻿using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core.ViewModels.Dialogs;
using System.Windows.Input;
using Tocronx.SimpleAsync;
using Windows.Storage;
using Windows.System;

namespace PhotoViewer.Core.Commands;

internal interface IMoveRawFilesToSubfolderCommand : IAcceleratedCommand { }

internal class MoveRawFilesToSubfolderCommand : AsyncCommandBase, IMoveRawFilesToSubfolderCommand
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

    protected override async Task ExecuteAsync()
    {
        await dialogService.ShowDialogAsync(new MoveRawFilesToSubfolderDialogModel(session.Files, settings));
    }
}
