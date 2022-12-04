using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using MetadataEditModule.ViewModel;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Messages;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using System.ComponentModel;
using Tocronx.SimpleAsync;

namespace PhotoViewerCore.ViewModels
{
    public delegate IMetadataPanelModel MetadataPanelModelFactory(bool tagPeopleOnPhotoButtonVisible);

    public interface IMetadataPanelModel : INotifyPropertyChanged
    {
        IReadOnlyList<IMediaFileInfo> Files { get; set; }

        bool IsVisible { get; set; }
    }

    public partial class MetadataPanelModel : ViewModelBase, IMetadataPanelModel
    {
        public bool IsVisible { get; set; } = true;

        public bool AreAllFilesSupported { get; private set; } = false;

        [DependsOn(nameof(AreAllFilesSupported))]
        public bool IsAnyFileNotSupported => !AreAllFilesSupported;

        public MetadataTextboxModel TitleTextboxModel { get; }

        public LocationSectionModel LocationSectionModel { get; }

        public PeopleSectionModel PeopleSectionModel { get; }

        public KeywordsSectionModel KeywordsSectionModel { get; }

        public RatingSectionModel RatingSectionModel { get; }

        public MetadataTextboxModel AuthorTextboxModel { get; }

        public MetadataTextboxModel CopyrightTextboxModel { get; }

        public DateTakenSectionModel DateTakenSectionModel { get; }

        public IReadOnlyList<IMediaFileInfo> Files { get; set; } = Array.Empty<IMediaFileInfo>();

        private IList<IBitmapFileInfo> supportedFiles = Array.Empty<IBitmapFileInfo>();

        private readonly IMessenger messenger;

        private readonly IMetadataService metadataService;

        private readonly CancelableTaskRunner updateRunner = new CancelableTaskRunner();

        private readonly SequentialTaskRunner writeFilesRunner = new SequentialTaskRunner();

        public MetadataPanelModel(IMessenger messenger, IMetadataService metadataService, LocationService locationService, bool tagPeopleOnPhotoButtonVisible)
        {
            this.messenger = messenger;
            this.metadataService = metadataService;

            TitleTextboxModel = new MetadataTextboxModel(writeFilesRunner, metadataService, MetadataProperties.Title);
            LocationSectionModel = new LocationSectionModel(locationService);
            PeopleSectionModel = new PeopleSectionModel(writeFilesRunner, messenger, metadataService, tagPeopleOnPhotoButtonVisible);
            KeywordsSectionModel = new KeywordsSectionModel(writeFilesRunner, messenger, metadataService);
            RatingSectionModel = new RatingSectionModel(writeFilesRunner, metadataService);
            AuthorTextboxModel = new MetadataTextboxModel(writeFilesRunner, metadataService, MetadataProperties.Author);
            CopyrightTextboxModel = new MetadataTextboxModel(writeFilesRunner, metadataService, MetadataProperties.Copyright);
            DateTakenSectionModel = new DateTakenSectionModel();
        }

        public override void OnViewConnected()
        {
            messenger.Subscribe<ToggleMetataPanelMessage>(OnToggleMetataPanelMessageReceived);
        }

        public override void OnViewDisconnected()
        {
            messenger.Unsubscribe<ToggleMetataPanelMessage>(OnToggleMetataPanelMessageReceived);
        }

        private void OnToggleMetataPanelMessageReceived(ToggleMetataPanelMessage msg)
        {
            IsVisible = !IsVisible;
        }

        partial void OnFilesChanged()
        {
            supportedFiles = Files.OfType<IBitmapFileInfo>().Where(file => file.IsMetadataSupported).ToList();

            AreAllFilesSupported = supportedFiles.Count == Files.Count;

            updateRunner.RunAndCancelPrevious(Update);
        }

        private async Task Update(CancellationToken cancellationToken)
        {
            if (AreAllFilesSupported)
            {
                var metadata = await Task.WhenAll(supportedFiles.Select(metadataService.GetMetadataAsync));
                cancellationToken.ThrowIfCancellationRequested();

                TitleTextboxModel.Update(supportedFiles, metadata);
                LocationSectionModel.UpdateAsync(metadata, cancellationToken);
                PeopleSectionModel.Update(supportedFiles, metadata);
                KeywordsSectionModel.Update(supportedFiles, metadata);
                RatingSectionModel.Update(supportedFiles, metadata);
                AuthorTextboxModel.Update(supportedFiles, metadata);
                CopyrightTextboxModel.Update(supportedFiles, metadata);
                DateTakenSectionModel.Update(metadata);
            }
            else
            {
                TitleTextboxModel.Clear();
                LocationSectionModel.Clear();
                PeopleSectionModel.Clear();
                KeywordsSectionModel.Clear();
                RatingSectionModel.Clear();
                AuthorTextboxModel.Clear();
                CopyrightTextboxModel.Clear();
                DateTakenSectionModel.Clear();
            }
        }


        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

    }
}
