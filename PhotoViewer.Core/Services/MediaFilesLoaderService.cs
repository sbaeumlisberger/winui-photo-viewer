﻿using PhotoViewer.App.Models;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.Storage.Search;
using FileAttributes = Windows.Storage.FileAttributes;

namespace PhotoViewer.App.Services;

public record class LoadMediaFilesResult(List<IMediaFileInfo> MediaFiles, IMediaFileInfo? StartMediaFile);

public interface IMediaFilesLoaderService
{
    LoadMediaFilesTask LoadFolder(IStorageFolder storageFolder, LoadMediaConfig config);
    LoadMediaFilesTask LoadFileList(List<IStorageFile> files, LoadMediaConfig config);
    LoadMediaFilesTask LoadNeighboringFilesQuery(IStorageFile startFile, IStorageQueryResultBase neighboringFilesQuery, LoadMediaConfig config);
    LoadMediaFilesTask LoadFromArguments(IList<string> arguments, LoadMediaConfig config);
}

public class MediaFilesLoaderService : IMediaFilesLoaderService
{
    private readonly IFileSystemService fileSystemService;

    private readonly ICachedImageLoaderService cachedImageLoaderService;

    public MediaFilesLoaderService(IFileSystemService? fileSystemService = null, ICachedImageLoaderService? cachedImageLoaderService = null)
    {
        this.fileSystemService = fileSystemService ?? new FileSystemService();
        this.cachedImageLoaderService = cachedImageLoaderService ?? CachedImageLoaderService.Instance;
    }

    public LoadMediaFilesTask LoadFolder(IStorageFolder storageFolder, LoadMediaConfig config)
    {
        var task = Task.Run(async () =>
        {
            var files = await fileSystemService.ListFilesAsync(storageFolder).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(config.RAWsFolderName))
            {
                string rawsFolderPath = Path.Combine(storageFolder.Path, config.RAWsFolderName);

                if (await fileSystemService.TryGetFolderAsync(rawsFolderPath).ConfigureAwait(false) is { } rawsFolder)
                {
                    files.AddRange(await fileSystemService.ListFilesAsync(rawsFolder).ConfigureAwait(false));
                }
            }

            var mediaFiles = ConvertFilesToMediaFiles(null, files, config);

            return new LoadMediaFilesResult(mediaFiles, null);
        });

        return new LoadMediaFilesTask(null, task);
    }

    public LoadMediaFilesTask LoadFileList(List<IStorageFile> files, LoadMediaConfig config)
    {
        var mediaFiles = ConvertFilesToMediaFiles(null, files, config);
        return new LoadMediaFilesTask(null, Task.FromResult(new LoadMediaFilesResult(mediaFiles, null)));
    }

    public LoadMediaFilesTask LoadNeighboringFilesQuery(IStorageFile startFile, IStorageQueryResultBase neighboringFilesQuery, LoadMediaConfig config)
    {
        var startMediaFile = GetStartMediaFile(startFile);
        return new LoadMediaFilesTask(startMediaFile, Task.Run(async () =>
        {
            var mediaFiles = await LoadMediaFilesFromFilesQueryAsync(startMediaFile, neighboringFilesQuery, config).ConfigureAwait(false);
            if (startMediaFile is null)
            {
                startMediaFile = FindStartMediaFile(mediaFiles, startFile);
            }
            return new LoadMediaFilesResult(mediaFiles, startMediaFile);
        }));
    }

    public LoadMediaFilesTask LoadFromArguments(IList<string> arguments, LoadMediaConfig config)
    {
        var filePaths = arguments.Where(fileSystemService.Exists);

        if (!filePaths.Any())
        {
            return LoadMediaFilesTask.Empty;
        }

        var startFile = fileSystemService.TryGetFileAsync(filePaths.First()).Result;

        var startMediaFile = startFile != null ? GetStartMediaFile(startFile) : null;

        return new LoadMediaFilesTask(startMediaFile, Task.Run(async () =>
        {
            var files = (await Task.WhenAll(filePaths.Select(path => fileSystemService.TryGetFileAsync(path)))).OfType<IStorageFile>().ToList();

            var mediaFiles = ConvertFilesToMediaFiles(startMediaFile, files, config);

            return new LoadMediaFilesResult(mediaFiles, startMediaFile);
        }));
    }

    private async Task<List<IMediaFileInfo>> LoadMediaFilesFromFilesQueryAsync(IMediaFileInfo? startMediaFile, IStorageQueryResultBase filesQuery, LoadMediaConfig config)
    {
        var sw = Stopwatch.StartNew();
        var files = await fileSystemService.ListFilesAsync(filesQuery).ConfigureAwait(false);
        sw.Stop();
        Log.Info($"filesQuery.GetFilesAsync took {sw.ElapsedMilliseconds}ms");

        if (!string.IsNullOrEmpty(config.RAWsFolderName))
        {
            string? directory = Path.GetDirectoryName(files.FirstOrDefault()?.Path);

            if (!string.IsNullOrEmpty(directory))
            {
                string rawsFolderPath = Path.Combine(directory, config.RAWsFolderName);

                if (await fileSystemService.TryGetFolderAsync(rawsFolderPath).ConfigureAwait(false) is { } rawsFolder)
                {
                    files.AddRange(await fileSystemService.ListFilesAsync(rawsFolder).ConfigureAwait(false));
                }
            }
        }

        return ConvertFilesToMediaFiles(startMediaFile, files, config);
    }

    private IMediaFileInfo? GetStartMediaFile(IStorageFile startFile)
    {
        string fileExtension = startFile.FileType.ToLower();

        if (startFile.Attributes.HasFlag(FileAttributes.Temporary)
            && startFile.Attributes.HasFlag(FileAttributes.ReadOnly)
            && startFile.Path.StartsWith(Path.GetTempPath()))
        {
            // file is probably a copy from a file accessed via MTP
            // the orginal file will be part of the neighboring files query
            return null;
        }
        else if (BitmapFileInfo.CommonFileExtensions.Contains(fileExtension))
        {
            var bitmapFileInfo = new BitmapFileInfo(startFile);

            // preload image
            cachedImageLoaderService.LoadFromFileAsync(bitmapFileInfo, CancellationToken.None);

            return bitmapFileInfo;
        }
        else if (BitmapFileInfo.RawFileExtensions.Contains(fileExtension))
        {
            // file might be linked to a common file    
            return null;
        }
        else if (VideoFileInfo.SupportedFileExtensions.Contains(fileExtension))
        {
            return new VideoFileInfo(startFile);
        }
        else if (VectorGraphicFileInfo.SupportedFileExtensions.Contains(fileExtension))
        {
            return new VectorGraphicFileInfo(startFile);
        }
        return null;
    }

    private List<IMediaFileInfo> ConvertFilesToMediaFiles(IMediaFileInfo? startFile, IReadOnlyList<IStorageFile> files, LoadMediaConfig config)
    {
        Stopwatch sw = Stopwatch.StartNew();

        bool linkRAWs = config.LinkRAWs;
        bool includeVideos = config.IncludeVideos || startFile is VideoFileInfo;

        List<IMediaFileInfo> mediaFiles = new List<IMediaFileInfo>();

        IList<IStorageFile> possibleLinkFiles = linkRAWs
            ? files.Where(file => GetLinkPrio(file.FileType) is not null).ToList()
            : Array.Empty<IStorageFile>();

        List<IStorageFile> rawFilesToLink = new List<IStorageFile>();

        foreach (var file in files)
        {
            string fileExtension = file.FileType.ToLower();

            if (startFile != null && file.IsEqual(startFile.StorageFile))
            {
                mediaFiles.Add(startFile);
            }
            else if (BitmapFileInfo.CommonFileExtensions.Contains(fileExtension))
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
                if (includeVideos)
                {
                    mediaFiles.Add(new VideoFileInfo(file));
                }
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
        Log.Info($"Convert files to media file info took {sw.ElapsedMilliseconds}ms");

        return mediaFiles;
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

    private bool CanRawFileBeLinked(IStorageFile rawFile, IList<IStorageFile> possibleLinkFiles)
    {
        string rawFileName = Path.GetFileNameWithoutExtension(rawFile.Name);
        return possibleLinkFiles.Any(file => Path.GetFileNameWithoutExtension(file.Name) == rawFileName);
    }

    private void LinkRawFiles(IList<IStorageFile> rawFilesToLink, IList<IMediaFileInfo> mediaFiles)
    {
        if (rawFilesToLink.Any())
        {
            IList<IBitmapFileInfo> possibleLinkBitmapFiles = mediaFiles.OfType<IBitmapFileInfo>()
                .Where(bitmapFile => GetLinkPrio(bitmapFile.FileExtension) is not null).ToList();

            foreach (var rawFile in rawFilesToLink)
            {
                string rawFileName = Path.GetFileNameWithoutExtension(rawFile.Name);
                var bitmapFile = possibleLinkBitmapFiles
                    .Where(bitmapFile => Path.GetFileNameWithoutExtension(bitmapFile.FileName) == rawFileName)
                    .OrderByDescending(bitmapFile => GetLinkPrio(bitmapFile.FileExtension))
                    .First();
                bitmapFile.LinkStorageFile(rawFile);
            }
        }
    }

    private int? GetLinkPrio(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".jpeg" => 100,
            ".jpe" => 90,
            ".jpg" => 80,
            ".jfif" => 70,
            ".tiff" => 60,
            ".tif" => 50,
            ".jxr" => 40,
            ".wdp" => 30,
            ".heic" => 20,
            ".heif" => 10,
            _ => null
        }; ;
    }
}
