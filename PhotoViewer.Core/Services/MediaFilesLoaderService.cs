using PhotoViewer.App.Models;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace PhotoViewer.App.Services;

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
        IMediaFileInfo? startMediaFile;

        if (activatedEventArgs is IFileActivatedEventArgsWithNeighboringFiles fileActivatedEventArgsWithNeighboringFiles)
        {
            var activatedFiles = fileActivatedEventArgsWithNeighboringFiles.Files.OfType<IStorageFile>().ToList();
            var neighboringFilesQuery = fileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery;

            IReadOnlyList<IStorageFile> files = activatedFiles;

            if (neighboringFilesQuery != null) // not always available
            {
                files = await neighboringFilesQuery.GetFilesAsync();
            }

            var startFile = activatedFiles.First();

            if (!string.IsNullOrEmpty(config.RAWsFolderName))
            {
                string? directory = Path.GetDirectoryName(files.FirstOrDefault()?.Path);

                if (!string.IsNullOrEmpty(directory))
                {
                    string rawsFolderPath = Path.Combine(directory, config.RAWsFolderName);

                    if (File.Exists(rawsFolderPath))
                    {
                        var rawsFolder = await StorageFolder.GetFolderFromPathAsync(rawsFolderPath).AsTask().ConfigureAwait(false);
                        files = files.Concat(await rawsFolder.GetFilesAsync().AsTask().ConfigureAwait(false)).ToList();
                    }
                }
            }

            mediaFiles = ConvertFilesToMediaFiles(files, config.LinkRAWs);
            startMediaFile = FindStartMediaFile(mediaFiles, startFile);
        }
        else if (activatedEventArgs is IFileActivatedEventArgs fileActivatedEventArgs)
        {
            mediaFiles = ConvertFilesToMediaFiles(fileActivatedEventArgs.Files.Cast<IStorageFile>().ToList(), config.LinkRAWs);
            startMediaFile = mediaFiles.FirstOrDefault();
        }
        else
        {
#if DEBUG
            return await LoadMediaFilesAsync(KnownFolders.PicturesLibrary, config);
#else
            mediaFiles =  new List<IMediaFileInfo>();
            startMediaFile = null;
#endif
        }
        return new LoadMediaFilesResult(mediaFiles, startMediaFile);
    }

    private IMediaFileInfo FindStartMediaFile(IReadOnlyList<IMediaFileInfo> mediaFiles, IStorageFile startFile) 
    {
        if (mediaFiles.FirstOrDefault(mediaFile =>
            mediaFile.StorageFile.IsEqual(startFile)) is IMediaFileInfo mediaFile) 
        {
            return mediaFile;
        }

        // The startFile can be a tempoary copy of one of the loaded files (e.g. when
        // accessing files on a smartphone or camera). Typically the file name of the
        // copy ends with a number inside brackets ("[" and "]").
        string assumedFileName = Regex.Replace(startFile.Name, "\\[\\d*\\]", "");
        return mediaFiles.FirstOrDefault(mediaFile => mediaFile.FileName == assumedFileName) 
            ?? mediaFiles.First(); // fallback to first file
    }

    public async Task<LoadMediaFilesResult> LoadMediaFilesAsync(StorageFolder storageFolder, LoadMediaConfig config)
    {
        var files = await storageFolder.GetFilesAsync().AsTask().ConfigureAwait(false);

        if (!string.IsNullOrEmpty(config.RAWsFolderName))
        {
            if (await storageFolder.TryGetItemAsync(config.RAWsFolderName).AsTask().ConfigureAwait(false) is IStorageFolder rawsFolder)
            {
                files = files.Concat(await rawsFolder.GetFilesAsync().AsTask().ConfigureAwait(false)).ToList();
            }
        }

        var mediaFiles = ConvertFilesToMediaFiles(files, config.LinkRAWs);
        var startMediaFile = mediaFiles.FirstOrDefault();
        return new LoadMediaFilesResult(mediaFiles, startMediaFile);
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
                mediaFiles.Add(new VideoFileInfo(file));
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
            IList<IBitmapFileInfo> possibleLinkFiles = mediaFiles.OfType<IBitmapFileInfo>().ToList();

            foreach (var rawFile in rawFilesToLink)
            {
                var mediaFileInfo = possibleLinkFiles.First(mfi => Path.GetFileNameWithoutExtension(mfi.FileName) == Path.GetFileNameWithoutExtension(rawFile.Name));
                mediaFileInfo.LinkStorageFile(rawFile);
            }
        }
    }

}
