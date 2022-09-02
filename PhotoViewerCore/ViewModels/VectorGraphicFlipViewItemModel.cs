using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public partial class VectorGraphicFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    [ObservableProperty]
    private string? content;

    [ObservableProperty]
    public bool isLoadingFailed = false;

    // TODO
    public IBitmapImage? BitmapImage => null;

    private TaskCompletionSource<bool> loadImageTaskCompletionSource = new TaskCompletionSource<bool>();

    public VectorGraphicFlipViewItemModel(IMediaFileInfo mediaFile)
    {
        MediaItem = mediaFile;
    }

    public async void StartLoading()
    {
        try
        {
            loadImageTaskCompletionSource = new TaskCompletionSource<bool>();
            string svgString = await FileIO.ReadTextAsync(MediaItem.File);
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
        catch (Exception exception)
        {
            Log.Error($"Could not load vector graphic {MediaItem.Name}", exception);
            IsLoadingFailed = true;
            loadImageTaskCompletionSource.SetResult(false);
        }
    }

    public void Cleanup()
    {
        Content = null;
        IsLoadingFailed = false;
    }

    // TODO
    public async Task<IBitmapImage?> WaitUntilImageLoaded()
    {
        await loadImageTaskCompletionSource.Task;
        return null;
    }
}
