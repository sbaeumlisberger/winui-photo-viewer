using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tocronx.SimpleAsync;
using Windows.Storage;

namespace PhotoViewerCore.ViewModels
{
    public partial class RatingSectionModel : ViewModelBase
    {
        public int Rating { get; set; }

        private readonly SequentialTaskRunner writeFilesRunner;

        private readonly IMetadataService metadataService;

        private IList<IBitmapFileInfo> files = Array.Empty<IBitmapFileInfo>();

        public RatingSectionModel(SequentialTaskRunner writeFilesRunner, IMetadataService metadataService)
        {
            this.writeFilesRunner = writeFilesRunner;
            this.metadataService = metadataService;
        }

        public void Update(IList<IBitmapFileInfo> files, IList<MetadataView> metadata)
        {
            this.files = files;
            var values = metadata.Select(m => m.Get(MetadataProperties.Rating)).ToList();
            bool allEqual = values.All(x => x == values.FirstOrDefault());
            Rating = allEqual ? values.FirstOrDefault() : 0;
        }

        public void Clear()
        {
            files = Array.Empty<IBitmapFileInfo>();
            Rating = 0;
        }

        async partial void OnRatingChanged()
        {
            if (!this.files.Any())
            {
                return;
            }

            try
            {
                await WriteFilesAsync(Rating, files.ToList());
            }
            catch (Exception e)
            {
                //  TODO handle errors and revert rating
                Log.Error("WriteFiles failed", e);
            }
        }

        private async Task WriteFilesAsync(int rating, IList<IBitmapFileInfo> files) 
        {
            await writeFilesRunner.Enqueue(async () =>
            {
                // TODO prallelize
                foreach (var file in files)
                {
                    if (!Equals(rating, await metadataService.GetMetadataAsync(file, MetadataProperties.Rating)))
                    {
                        await metadataService.WriteMetadataAsync(file, MetadataProperties.Rating, rating);
                    }
                }
            });
        }

    }
}
