using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

        private IReadOnlyList<IBitmapFileInfo> supportedFiles = Array.Empty<IBitmapFileInfo>();

        private readonly IMetadataService metadataService;

        private readonly CancelableTaskRunner updateRunner = new CancelableTaskRunner();

        private readonly SequentialTaskRunner writeFilesRunner = new SequentialTaskRunner();

        public MetadataPanelModel(
            IMessenger messenger,
            IMetadataService metadataService,
            ILocationService locationService,
            IDialogService dialogService,
            IClipboardService clipboardService,
            bool tagPeopleOnPhotoButtonVisible) : base(messenger)
        {
            this.metadataService = metadataService;

            TitleTextboxModel = new MetadataTextboxModel(writeFilesRunner, metadataService, MetadataProperties.Title);
            LocationSectionModel = new LocationSectionModel(writeFilesRunner, metadataService, locationService, dialogService, clipboardService, messenger);
            PeopleSectionModel = new PeopleSectionModel(writeFilesRunner, messenger, metadataService, tagPeopleOnPhotoButtonVisible);
            KeywordsSectionModel = new KeywordsSectionModel(writeFilesRunner, messenger, metadataService);
            RatingSectionModel = new RatingSectionModel(writeFilesRunner, metadataService);
            AuthorTextboxModel = new MetadataTextboxModel(writeFilesRunner, metadataService, MetadataProperties.Author);
            CopyrightTextboxModel = new MetadataTextboxModel(writeFilesRunner, metadataService, MetadataProperties.Copyright);
            DateTakenSectionModel = new DateTakenSectionModel();

            Messenger.Register<ToggleMetataPanelMessage>(this, OnReceive);
            Messenger.Register<MetadataModifiedMessage>(this, OnReceive);
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
            // TODO DateTakenSectionModel.UpdateMetadataModified(msg.MetadataProperty);
        }

        partial void OnFilesChanged()
        {
            supportedFiles = Files.OfType<IBitmapFileInfo>().Where(file => file.IsMetadataSupported).ToList();

            AreAllFilesSupported = supportedFiles.Count == Files.Count;

            updateRunner.RunAndCancelPrevious(Update);
        }

        private async Task Update(CancellationToken cancellationToken)
        {
            var metadata = await Task.WhenAll(supportedFiles.Select(metadataService.GetMetadataAsync));
            cancellationToken.ThrowIfCancellationRequested();

            TitleTextboxModel.UpdateFilesChanged(supportedFiles, metadata);
            LocationSectionModel.UpdateFilesChanged(supportedFiles, metadata);
            PeopleSectionModel.UpdateFilesChanged(supportedFiles, metadata);
            KeywordsSectionModel.UpdateFilesChanged(supportedFiles, metadata);
            RatingSectionModel.UpdateFilesChanged(supportedFiles, metadata);
            AuthorTextboxModel.UpdateFilesChanged(supportedFiles, metadata);
            CopyrightTextboxModel.UpdateFilesChanged(supportedFiles, metadata);
            DateTakenSectionModel.Update(metadata);
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

    }
}
