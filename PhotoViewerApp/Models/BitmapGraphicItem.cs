using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewerApp.Models
{
    public class BitmapGraphicItem : IMediaItem
    {
        public string Name => File.Name;

        public IStorageFile File { get; }

        public bool IsMetadataSupported { get; } = false;

        private DateTimeOffset? dateModified;

        private ulong? fileSize;

        public BitmapGraphicItem(IStorageFile storageFile)
        {
            File = storageFile;
        }

        public async Task<DateTimeOffset> GetDateModifiedAsync()
        {
            if (dateModified is null)
            {
                await LoadBasicPropertiesAsync().ConfigureAwait(false);
            }
            return (DateTimeOffset)dateModified!;

        }

        public async Task<ulong> GetFileSizeAsync()
        {
            if (fileSize is null)
            {
                await LoadBasicPropertiesAsync().ConfigureAwait(false);
            }
            return (ulong)fileSize!;
        }

        public async Task DeleteAsync()
        {
            await File.DeleteAsync().AsTask().ConfigureAwait(false);
        }

        private async Task LoadBasicPropertiesAsync() 
        {
            var basicProperties = await File.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
            dateModified = basicProperties.DateModified;
            fileSize = basicProperties.Size;
        }

    }
}
