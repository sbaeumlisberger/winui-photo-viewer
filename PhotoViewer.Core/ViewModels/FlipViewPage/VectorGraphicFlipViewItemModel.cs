using Essentials.NET;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using Essentials.NET.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using Windows.Storage;

namespace PhotoViewer.App.ViewModels;

public partial class VectorGraphicFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaFile { get; }

    public bool IsSelected { get; set; } = false;

    public bool IsDiashowActive { get; set; } = false;

    public string? Content { get; private set; }

    public bool IsLoadingFailed { get; private set; } = false;

    public bool IsContextMenuEnabeld => IsSelected && !IsDiashowActive;

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    private readonly CancelableTaskRunner initRunner = new CancelableTaskRunner();

    public VectorGraphicFlipViewItemModel(IMediaFileInfo mediaFile, IViewModelFactory viewModelFactory) : base(null!)
    {
        MediaFile = mediaFile;
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel();
        ContextMenuModel.Files = new[] { mediaFile };
    }

    partial void OnIsContextMenuEnabeldChanged()
    {
        ContextMenuModel.IsEnabled = IsContextMenuEnabeld;
    }

    public async Task InitializeAsync()
    {
        await initRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            try
            {
                IsLoadingFailed = false;
                using var fileStream = await MediaFile.OpenAsync(FileAccessMode.Read);
                cancellationToken.ThrowIfCancellationRequested();
                string svgString = await new StreamReader(fileStream).ReadToEndAsync();
                cancellationToken.ThrowIfCancellationRequested();
                Content = SvgUtil.EmbedInHtml(svgString);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"Could not load vector graphic {MediaFile.DisplayName}", ex);
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
