using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using MetadataEditModule.ViewModel;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;
using Tocronx.SimpleAsync;

namespace PhotoViewer.Core.ViewModels
{
    public delegate IMetadataPanelModel MetadataPanelModelFactory(bool tagPeopleOnPhotoButtonVisible);

    public interface IMetadataPanelModel : IViewModel
    {
        IReadOnlyList<IMediaFileInfo> Files { get; set; }

        bool IsVisible { get; set; }
    }

    public partial class MetadataPanelModel : ViewModelBase, IMetadataPanelModel
    {
        public bool IsVisible { get; set; } = false;

        public bool IsLoading { get; set; } = false;

        public bool IsLoaded => !IsLoading;

        public bool IsNoFilesSelectedMessageVisible { get; private set; } = true;

        public bool IsInputVisible { get; private set; } = false;

        public bool IsUnsupportedFilesMessageVisibile { get; private set; } = false;

        public MetadataTextboxModel TitleTextboxModel { get; }
        public LocationSectionModel LocationSectionModel { get; }
        public PeopleSectionModel PeopleSectionModel { get; }
        public KeywordsSectionModel KeywordsSectionModel { get; }
        public RatingSectionModel RatingSectionModel { get; }
        public MetadataTextboxModel AuthorTextboxModel { get; }
        public MetadataTextboxModel CopyrightTextboxModel { get; }
        public DateTakenSectionModel DateTakenSectionModel { get; }

        public IReadOnlyList<IMediaFileInfo> Files { get; set; } = Array.Empty<IMediaFileInfo>();

        private IReadOnlyList<IBitmapFileInfo> supportedFiles = Array.Empty<IBitmapFileInfo>();

        private readonly IMetadataService metadataService;

        private readonly CancelableTaskRunner updateRunner = new CancelableTaskRunner();

        internal MetadataPanelModel(
            IMessenger messenger,
            IMetadataService metadataService,
            ILocationService locationService,
            IDialogService dialogService,
            IViewModelFactory viewModelFactory,
            ISuggestionsService peopleSuggestionsService,
            ISuggestionsService keywordSuggestionsService,
            IGpxService gpxService,
            IBackgroundTaskService backgroundTaskService,
            ApplicationSettings applicationSettings,
            bool tagPeopleOnPhotoButtonVisible) : base(messenger)
        {
            this.metadataService = metadataService;

            TitleTextboxModel = new MetadataTextboxModel(metadataService, dialogService, backgroundTaskService, MetadataProperties.Title);
            LocationSectionModel = new LocationSectionModel(metadataService, locationService, dialogService, viewModelFactory, gpxService, messenger, backgroundTaskService);
            PeopleSectionModel = new PeopleSectionModel(messenger, metadataService, peopleSuggestionsService, dialogService, backgroundTaskService, tagPeopleOnPhotoButtonVisible);
            KeywordsSectionModel = new KeywordsSectionModel(messenger, metadataService, keywordSuggestionsService, dialogService, backgroundTaskService);
            RatingSectionModel = new RatingSectionModel(metadataService, dialogService, messenger, backgroundTaskService);
            AuthorTextboxModel = new MetadataTextboxModel(metadataService, dialogService, backgroundTaskService, MetadataProperties.Author);
            CopyrightTextboxModel = new MetadataTextboxModel(metadataService, dialogService, backgroundTaskService, MetadataProperties.Copyright);
            DateTakenSectionModel = new DateTakenSectionModel(messenger, metadataService, dialogService, backgroundTaskService);

            IsVisible = applicationSettings.AutoOpenMetadataPanel;

            Register<ToggleMetataPanelMessage>(OnReceive);
            Register<MetadataModifiedMessage>(OnReceive);
        }

        protected override void OnCleanup()
        {
            TitleTextboxModel.Cleanup();
            LocationSectionModel.Cleanup();
            PeopleSectionModel.Cleanup();
            KeywordsSectionModel.Cleanup();
            RatingSectionModel.Cleanup();
            AuthorTextboxModel.Cleanup();
            CopyrightTextboxModel.Cleanup();
            DateTakenSectionModel.Cleanup();
        }

        private void OnReceive(ToggleMetataPanelMessage msg)
        {
            IsVisible = !IsVisible;
        }

        private void OnReceive(MetadataModifiedMessage msg)
        {
            TitleTextboxModel.UpdateMetadataModified(msg.MetadataProperty);
            LocationSectionModel.UpdateMetadataModified(msg.MetadataProperty);
            PeopleSectionModel.UpdateMetadataModified(msg.MetadataProperty);
            KeywordsSectionModel.UpdateMetadataModified(msg.MetadataProperty);
            RatingSectionModel.UpdateMetadataModified(msg.MetadataProperty);
            AuthorTextboxModel.UpdateMetadataModified(msg.MetadataProperty);
            CopyrightTextboxModel.UpdateMetadataModified(msg.MetadataProperty);
            DateTakenSectionModel.UpdateMetadataModified(msg.MetadataProperty);
        }

        partial void OnFilesChanged()
        {
            supportedFiles = Files.OfType<IBitmapFileInfo>().Where(file => file.IsMetadataSupported).ToList();

            IsNoFilesSelectedMessageVisible = !Files.Any();

            bool allFilsSupported = supportedFiles.Count == Files.Count;

            IsInputVisible = Files.Any() && allFilsSupported;
            IsUnsupportedFilesMessageVisibile = Files.Any() && !allFilsSupported;

            updateRunner.RunAndCancelPrevious(Update);
        }

        private async Task Update(CancellationToken cancellationToken)
        {
            IsLoading = true;

            try
            {
                var metadata = await supportedFiles.MapParallelAsync(metadataService.GetMetadataAsync, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                TitleTextboxModel.UpdateFilesChanged(supportedFiles, metadata);
                LocationSectionModel.UpdateFilesChanged(supportedFiles, metadata);
                PeopleSectionModel.UpdateFilesChanged(supportedFiles, metadata);
                KeywordsSectionModel.UpdateFilesChanged(supportedFiles, metadata);
                RatingSectionModel.UpdateFilesChanged(supportedFiles, metadata);
                AuthorTextboxModel.UpdateFilesChanged(supportedFiles, metadata);
                CopyrightTextboxModel.UpdateFilesChanged(supportedFiles, metadata);
                DateTakenSectionModel.UpdateFilesChanged(supportedFiles, metadata);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) 
            {
                // TODO show dialog
                Log.Error("Failed to update metadata panel", ex);
            }
           
            IsLoading = false;
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

    }
}
