using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using MetadataAPI;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;

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

        internal RatingSectionModel(
            IMetadataService metadataService,
            IDialogService dialogService,
            IMessenger messenger,
            IBackgroundTaskService backgroundTaskService) : base(messenger, backgroundTaskService, dialogService)
        {
            this.metadataService = metadataService;
            this.dialogService = dialogService;
        }

        protected override void OnFilesChanged(IReadOnlyList<MetadataView> metadata)
        {
            Update(metadata);
        }

        protected override void OnMetadataModified(IReadOnlyList<MetadataView> metadata, IMetadataProperty metadataProperty)
        {
            if (metadataProperty == MetadataProperties.Rating)
            {
                Update(metadata);
            }
        }

        public void Update(IReadOnlyList<MetadataView> metadata)
        {
            var values = metadata.Select(m => m.Get(MetadataProperties.Rating)).ToList();
            rating = values.AllEqual() ? values.FirstOrDefault() : 0;
            OnPropertyChanged(nameof(Rating));
        }

        private async void OnRatingChangedExternal()
        {
            await WriteFilesAsync(Rating);
        }

        private async Task WriteFilesAsync(int rating)
        {
            await WriteFilesAndCancelPreviousAsync(async (file, cancellationToken) =>
            {
                if (!Equals(rating, await metadataService.GetMetadataAsync(file, MetadataProperties.Rating).ConfigureAwait(false)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await metadataService.WriteMetadataAsync(file, MetadataProperties.Rating, rating).ConfigureAwait(false);
                }
            },
            (processedFiles) =>
            {
                Messenger.Send(new MetadataModifiedMessage(processedFiles, MetadataProperties.Rating));
            });
        }

    }
}
