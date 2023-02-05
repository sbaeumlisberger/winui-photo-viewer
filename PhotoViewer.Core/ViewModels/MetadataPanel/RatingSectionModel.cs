using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Utils;
using Tocronx.SimpleAsync;

namespace PhotoViewer.Core.ViewModels
{
    public partial class RatingSectionModel : MetadataPanelSectionModelBase
    {
        public int Rating
        {
            get => rating;
            set
            {
                if (SetProperty(ref rating, value))
                {
                    OnRatingChangedExternal();
                }
            }
        }
        private int rating = 0;

        private readonly IMetadataService metadataService;

        public RatingSectionModel(SequentialTaskRunner writeFilesRunner, IMetadataService metadataService)
            : base(writeFilesRunner, null!)
        {
            this.metadataService = metadataService;
        }

        protected override void OnFilesChanged(IList<MetadataView> metadata)
        {
            Update(metadata);
        }

        protected override void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty)
        {
            if (metadataProperty == MetadataProperties.Rating)
            {
                Update(metadata);
            }
        }

        public void Update(IList<MetadataView> metadata)
        {           
            var values = metadata.Select(m => m.Get(MetadataProperties.Rating)).ToList();
            bool allEqual = values.All(x => x == values.FirstOrDefault());
            rating = allEqual ? values.FirstOrDefault() : 0;
            OnPropertyChanged(nameof(Rating));
        }

        private async void OnRatingChangedExternal()
        {
            await WriteFilesAsync(Rating);            
        }

        private async Task WriteFilesAsync(int rating) 
        {
            await EnqueueWriteFiles(async (files) =>
            {
                var result = await ParallelProcessingUtil.ProcessParallelAsync(files, async file =>
                {                
                    if (!Equals(rating, await metadataService.GetMetadataAsync(file, MetadataProperties.Rating).ConfigureAwait(false)))
                    {
                        await metadataService.WriteMetadataAsync(file, MetadataProperties.Rating, rating).ConfigureAwait(false);
                    }
                });

                if (result.HasFailures) 
                { 
                    // TODO show error
                }
            });
        }

    }
}
