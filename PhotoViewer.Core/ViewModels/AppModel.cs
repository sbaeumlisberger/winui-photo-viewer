using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Models;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace PhotoViewer.Core.ViewModels;

public class AppModel
{
    private readonly ApplicationSettings settings;

    private readonly IMessenger messenger;

    private readonly IViewModelFactory viewModelFactory;

    private readonly IMediaFilesLoaderService mediaFilesLoaderService;

    public AppModel(
        ApplicationSettings settings,
        IMessenger messenger,
        IViewModelFactory viewModelFactory,
        IMediaFilesLoaderService mediaFilesLoaderService)
    {
        this.settings = settings;
        this.messenger = messenger;
        this.viewModelFactory = viewModelFactory;
        this.mediaFilesLoaderService = mediaFilesLoaderService;
    }



    private LoadMediaFilesTask LoadMediaFiles(IActivatedEventArgs activatedEventArgs)
    {
        Log.Info($"Load files from {activatedEventArgs}");

        var config = new LoadMediaConfig(settings.LinkRawFiles, settings.RawFilesFolderName, settings.IncludeVideos);

        if (activatedEventArgs is IFileActivatedEventArgsWithNeighboringFiles fileActivatedEventArgsWithNeighboringFiles)
        {
            if (fileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery is { } neighboringFilesQuery)
            {
                var startFile = (IStorageFile)fileActivatedEventArgsWithNeighboringFiles.Files[0];
                return mediaFilesLoaderService.LoadNeighboringFilesQuery(startFile, neighboringFilesQuery, config);
            }
            else
            {
                var activatedFiles = fileActivatedEventArgsWithNeighboringFiles.Files.Cast<IStorageFile>().ToList();
                return mediaFilesLoaderService.LoadFileList(activatedFiles, config);
            }
        }
        else if (activatedEventArgs is IFileActivatedEventArgs fileActivatedEventArgs)
        {
            var activatedFiles = fileActivatedEventArgs.Files.Cast<IStorageFile>().ToList();
            return mediaFilesLoaderService.LoadFileList(activatedFiles, config);
        }
        else if (Environment.GetCommandLineArgs().Length > 1)
        {
            var arguments = Environment.GetCommandLineArgs().Skip(1).ToList();
            return mediaFilesLoaderService.LoadFromArguments(arguments, config);
        }
        else if (Debugger.IsAttached)
        {
            var folder = KnownFolders.SavedPictures;
            return mediaFilesLoaderService.LoadFolder(folder, config);
        }
        else
        {
#if DEBUG
            return mediaFilesLoaderService.LoadFolder(KnownFolders.PicturesLibrary, config);
#else
            var folder = KnownFolders.SavedPictures;
            return mediaFilesLoaderService.LoadFolder(folder, config);
            //return LoadMediaFilesTask.Empty;
#endif
        }
    }

}
