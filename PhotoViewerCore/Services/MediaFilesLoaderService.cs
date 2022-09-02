using PhotoViewerApp.Models;
using PhotoViewerApp.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace PhotoViewerApp.Services;

public record class LoadMediaFilesResult(List<IMediaFileInfo> MediaItems, IMediaFileInfo? StartItem);

public interface IMediaFilesLoaderService
{
    Task<LoadMediaFilesResult> LoadMediaFilesAsync(IActivatedEventArgs activatedEventArgs, LoadMediaConfig config);
    Task<LoadMediaFilesResult> LoadMediaFilesAsync(StorageFolder storageFolder, LoadMediaConfig config);
}

public class MediaFilesLoaderService : IMediaFilesLoaderService
{

    public async Task<LoadMediaFilesResult> LoadMediaFilesAsync(IActivatedEventArgs activatedEventArgs, LoadMediaConfig config)
    {
        List<IMediaFileInfo> mediaFiles;
        IMediaFileInfo? startFile;

        if (activatedEventArgs is IFileActivatedEventArgsWithNeighboringFiles fileActivatedEventArgsWithNeighboringFiles)
        {
            var files = await fileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery.GetFilesAsync();

            if (config.RAWsFolderName != null)
            {
                string rawsFolderPath = Path.Combine(Path.GetDirectoryName(files.First().Path)!, config.RAWsFolderName);

                if (File.Exists(rawsFolderPath))
                {
                    var rawsFolder = await StorageFolder.GetFolderFromPathAsync(rawsFolderPath).AsTask().ConfigureAwait(false);
                    files = files.Concat(await rawsFolder.GetFilesAsync().AsTask().ConfigureAwait(false)).ToList();
                }
            }

            mediaFiles = ConvertFilesToMediaFiles(files, config.LinkRAWs);
            startFile = mediaFiles.First(mediaItem => mediaItem.File.Path == fileActivatedEventArgsWithNeighboringFiles.Files.First().Path);
        }
        else if (activatedEventArgs is IFileActivatedEventArgs fileActivatedEventArgs)
        {
            mediaFiles = ConvertFilesToMediaFiles(fileActivatedEventArgs.Files.Cast<IStorageFile>().ToList(), config.LinkRAWs);
            startFile = mediaFiles.First();
        }
        else
        {
#if DEBUG
            return await LoadMediaFilesAsync(KnownFolders.PicturesLibrary, config);
#else
            mediaItems =  new List<IMediaItem>();
            startItem = null;
#endif
        }
        return new LoadMediaFilesResult(mediaFiles, startFile);
    }

    public async Task<LoadMediaFilesResult> LoadMediaFilesAsync(StorageFolder storageFolder, LoadMediaConfig config)
    {
        var files = await storageFolder.GetFilesAsync().AsTask().ConfigureAwait(false);

        if (config.RAWsFolderName != null)
        {
            if (await storageFolder.TryGetItemAsync(config.RAWsFolderName).AsTask().ConfigureAwait(false) is IStorageFolder rawsFolder)
            {
                files = files.Concat(await rawsFolder.GetFilesAsync().AsTask().ConfigureAwait(false)).ToList();
            }
        }

        var mediaFiles = ConvertFilesToMediaFiles(files, config.LinkRAWs);
        var startFile = mediaFiles.FirstOrDefault();
        return new LoadMediaFilesResult(mediaFiles, startFile);
    }

    private List<IMediaFileInfo> ConvertFilesToMediaFiles(IReadOnlyList<IStorageFile> files, bool linkRAWs)
    {
        Stopwatch sw = Stopwatch.StartNew();

        List<IMediaFileInfo> mediaFiles = new List<IMediaFileInfo>();

        IList<IStorageFile> possibleLinkFiles = linkRAWs
            ? files.Where(file => BitmapFileInfo.CommonFileExtensions.Contains(file.FileType.ToLower())).ToList()
            : Array.Empty<IStorageFile>();

        List<IStorageFile> rawFilesToLink = new List<IStorageFile>();

        foreach (var file in files)
        {
            string fileExtension = file.FileType.ToLower();

            if (BitmapFileInfo.CommonFileExtensions.Contains(fileExtension))
            {
                mediaFiles.Add(new BitmapFileInfo(file));
            }
            else if (BitmapFileInfo.RawFileExtensions.Contains(fileExtension))
            {
                if (linkRAWs && CanRawFileBeLinked(file, possibleLinkFiles))
                {
                    rawFilesToLink.Add(file);
                }
                else
                {
                    mediaFiles.Add(new BitmapFileInfo(file));
                }
            }
            else if (VideoFileInfo.SupportedFileExtensions.Contains(fileExtension))
            {
                // not yet supported
            }
            else if (VectorGraphicFileInfo.SupportedFileExtensions.Contains(fileExtension))
            {
                mediaFiles.Add(new VectorGraphicFileInfo(file));
            }
        }

        if (linkRAWs)
        {
            LinkRawFiles(rawFilesToLink, mediaFiles);
        }

        sw.Stop();
        Log.Debug($"Convert files to media file info took {sw.ElapsedMilliseconds}ms");

        return mediaFiles;
    }

    private bool CanRawFileBeLinked(IStorageFile rawFile, IList<IStorageFile> possibleLinkFiles)
    {
        string rawFileName = Path.GetFileNameWithoutExtension(rawFile.Name);
        return possibleLinkFiles.Any(file => Path.GetFileNameWithoutExtension(file.Name) == rawFileName);
    }

    private void LinkRawFiles(IList<IStorageFile> rawFilesToLink, IList<IMediaFileInfo> mediaFiles)
    {
        if (rawFilesToLink.Any())
        {
            IList<BitmapFileInfo> possibleLinkFiles = mediaFiles.OfType<BitmapFileInfo>().ToList();

            foreach (var rawFile in rawFilesToLink)
            {
                var mediaFileInfo = possibleLinkFiles.First(mfi => Path.GetFileNameWithoutExtension(mfi.File.Name) == Path.GetFileNameWithoutExtension(rawFile.Name));
                mediaFileInfo.LinkedFiles.Add(rawFile);
            }
        }
    }

}
