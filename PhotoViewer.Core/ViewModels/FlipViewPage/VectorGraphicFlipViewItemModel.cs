﻿using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Text;
using Tocronx.SimpleAsync;
using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public partial class VectorGraphicFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsSelected { get; set; } = false;

    public bool IsDiashowActive { get; set; } = false;

    public string? Content { get; private set; }

    public bool IsLoadingFailed { get; private set; } = false;

    public bool IsContextMenuEnabeld => !IsDiashowActive;

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    private readonly CancelableTaskRunner initRunner = new CancelableTaskRunner();

    public VectorGraphicFlipViewItemModel(IMediaFileInfo mediaFile, IViewModelFactory viewModelFactory) : base(null!)
    {
        MediaItem = mediaFile;
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel();
        ContextMenuModel.Files = new[] { mediaFile };
    }

    public async Task InitializeAsync()
    {
        await initRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            try
            {
                IsLoadingFailed = false;
                using var fileStream = await MediaItem.OpenAsync(FileAccessMode.Read);
                cancellationToken.ThrowIfCancellationRequested();
                string svgString = await new StreamReader(fileStream).ReadToEndAsync();
                cancellationToken.ThrowIfCancellationRequested();
                Content = SvgUtil.EmbedInHtml(svgString);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"Could not load vector graphic {MediaItem.DisplayName}", ex);
                IsLoadingFailed = true;
            }
        });
    }

    protected override void OnCleanup()
    {
        initRunner.Cancel();
        Content = null;
        IsLoadingFailed = false;
        ContextMenuModel.Cleanup();
    }

}
