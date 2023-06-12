using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Services;
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
        private readonly IDialogService dialogService;

        public RatingSectionModel(
            IMetadataService metadataService,
            IDialogService dialogService,
            IMessenger messenger,
            IBackgroundTaskService backgroundTaskService) : base(messenger, backgroundTaskService, dialogService)
        {
            this.metadataService = metadataService;
            this.dialogService = dialogService;
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
            var result = await WriteFilesAsync(async file =>
            {
                 if (!Equals(rating, await metadataService.GetMetadataAsync(file, MetadataProperties.Rating).ConfigureAwait(false)))
                 {
                     await metadataService.WriteMetadataAsync(file, MetadataProperties.Rating, rating).ConfigureAwait(false);
                 }
             });

            Messenger.Send(new MetadataModifiedMessage(result.ProcessedElements, MetadataProperties.Rating));
        }

    }
}
