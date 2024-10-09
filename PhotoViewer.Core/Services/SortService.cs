using Essentials.NET;
using MetadataAPI;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Services;

internal enum SortBy
{
    Unspecified,
    FileName,
    FileSize,
    DateTaken,
}

internal class SortService
{
    private readonly IMetadataService metadataService;

    public SortService(IMetadataService metadataService)
    {
        this.metadataService = metadataService;
    }

    public async Task<List<IMediaFileInfo>> SortAsync(IReadOnlyList<IMediaFileInfo> mediaFiles, SortBy sortBy, bool descending = false)
    {
        switch (sortBy)
        {
            case SortBy.Unspecified:
                return mediaFiles.ToList();
            case SortBy.FileName:
                return doSort(file => file.FileName);
            case SortBy.FileSize:
                var fileSizeByFile = await GetFileSizeDictionaryAsync(mediaFiles);
                return doSort(file => fileSizeByFile[file]);
            case SortBy.DateTaken:
                var dateTakenByFile = await GetDateTakenDictionaryAsync(mediaFiles);
                return doSort(file => dateTakenByFile[file]);
            default:
                throw new ArgumentOutOfRangeException(nameof(sortBy));
        }

        List<IMediaFileInfo> doSort(Func<IMediaFileInfo, object> selector)
        {
            return descending
                ? mediaFiles.OrderByDescending(selector).ToList()
                : mediaFiles.OrderBy(selector).ToList();
        }
    }

    private async Task<Dictionary<IMediaFileInfo, ulong>> GetFileSizeDictionaryAsync(IReadOnlyList<IMediaFileInfo> mediaFiles)
    {
        var fileSizeByFile = new Dictionary<IMediaFileInfo, ulong>();

        foreach (var file in mediaFiles)
        {
            fileSizeByFile[file] = await file.GetFileSizeAsync();
        }

        return fileSizeByFile;
    }

    private async Task<Dictionary<IMediaFileInfo, DateTimeOffset>> GetDateTakenDictionaryAsync(IReadOnlyList<IMediaFileInfo> mediaFiles)
    {
        var dateTakenByFile = new Dictionary<IMediaFileInfo, DateTimeOffset>();

        var dateTakenList = await mediaFiles.Parallel().ProcessAsync(GetDateTakenAsync);

        for (int i = 0; i < mediaFiles.Count; i++)
        {
            dateTakenByFile[mediaFiles[i]] = dateTakenList[i];
        }

        return dateTakenByFile;
    }

    private async Task<DateTimeOffset> GetDateTakenAsync(IMediaFileInfo file)
    {
        if (file is IBitmapFileInfo bitmapFile && bitmapFile.IsMetadataSupported
            && await metadataService.GetMetadataAsync(bitmapFile, MetadataProperties.DateTaken) is { } dateTaken)
        {
            return new DateTimeOffset(dateTaken);
        }
        return await file.GetDateModifiedAsync(); // fallback to date modified
    }

}
