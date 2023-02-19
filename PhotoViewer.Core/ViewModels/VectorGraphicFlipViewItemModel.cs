using CommunityToolkit.Mvvm.ComponentModel;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
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

    private readonly CancelableTaskRunner initRunner = new CancelableTaskRunner();

    public VectorGraphicFlipViewItemModel(IMediaFileInfo mediaFile)
    {
        MediaItem = mediaFile;
    }

    public async Task PrepareAsync()
    {
        await initRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            try
            {
                IsLoadingFailed = false;
                using var fileStream = await MediaItem.OpenAsync(FileAccessMode.Read);
                cancellationToken.ThrowIfCancellationRequested();
                string svgString = await new StreamReader(fileStream.AsStream()).ReadToEndAsync();
                cancellationToken.ThrowIfCancellationRequested();
                Content = "<html>\n" +
                    "   <head>\n" +
                    "       <style>\n" +
                    "           body {\n" +
                    "               margin: 0px;\n" +
                    "           }\n" +
                    "           \n" +
                    "           ::-webkit-scrollbar {\n" +
                    "               display: none;\n" +
                    "           }\n" +
                    "           \n" +
                    "           svg {\n" +
                    "               max-width: 100%;\n" +
                    "               max-height: 100%;\n" +
                    "               position: absolute;\n" +
                    "               top: 50%;\n" +
                    "               left: 50%;\n" +
                    "               translate: -50% -50%;\n" +
                    "           }\n" +
                    "       </style>\n" +
                    "   </head>\n" +
                    "   <body>\n" +
                    "       " + svgString + "\n" +
                    "   </body>\n" +
                    "</html>\n";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error($"Could not load vector graphic {MediaItem.DisplayName}", ex);
                IsLoadingFailed = true;
            }
        });
    }

    public void Cleanup()
    {
        initRunner.Cancel();
        Content = null;
        IsLoadingFailed = false;
    }

}
