using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Messages;
using System.ComponentModel;
using Tocronx.SimpleAsync;

namespace PhotoViewerCore.ViewModels
{
    public delegate IMetadataPanelModel MetadataPanelModelFactory(bool tagPeopleOnPhotoButtonVisible);

    public interface IMetadataPanelModel : INotifyPropertyChanged
    {
        IList<IMediaFileInfo> Files { get; set; }

        bool IsVisible { get; set; }
    }

    public partial class MetadataPanelModel : ViewModelBase, IMetadataPanelModel
    {
        public bool IsVisible { get; set; } = true;

        public bool IsEnabled { get; private set; } = false;

        public MetadataTextboxModel TitleTextboxModel { get; }

        public KeywordsSectionModel KeywordsSectionModel { get; }

        public MetadataTextboxModel AuthorTextboxModel { get; }

        public MetadataTextboxModel CopyrightTextboxModel { get; }

        public IList<IMediaFileInfo> Files { get; set; } = Array.Empty<IMediaFileInfo>();

        private IList<IBitmapFileInfo> supportedFiles = Array.Empty<IBitmapFileInfo>();

        public bool IsTagPeopleOnPhotoButtonVisible { get; }

        public bool IsTagPeopleOnPhotoButtonChecked { get; private set; }

        private readonly IMessenger messenger;

        private readonly IMetadataService metadataService;

        private readonly CancelableTaskRunner updateRunner = new CancelableTaskRunner();

        private readonly SequentialTaskRunner writeFilesRunner = new SequentialTaskRunner();

        public MetadataPanelModel(IMessenger messenger, IMetadataService metadataService, bool tagPeopleOnPhotoButtonVisible)
        {
            this.messenger = messenger;
            this.metadataService = metadataService;

            IsTagPeopleOnPhotoButtonVisible = tagPeopleOnPhotoButtonVisible;

            if (IsTagPeopleOnPhotoButtonVisible)
            {
                messenger.Subscribe<TagPeopleToolActiveChanged>(msg =>
                {
                    IsTagPeopleOnPhotoButtonChecked = msg.IsVisible;
                });
            }

            TitleTextboxModel = new MetadataTextboxModel(value => WriteMetadataAsync(MetadataProperties.Title, value));
            KeywordsSectionModel = new KeywordsSectionModel(metadataService);
            AuthorTextboxModel = new MetadataTextboxModel(value => WriteMetadataAsync(MetadataProperties.Author, value.Split(";").Select(author => author.Trim()).ToArray()));
            CopyrightTextboxModel = new MetadataTextboxModel(value => WriteMetadataAsync(MetadataProperties.Copyright, value));
        }

        partial void OnFilesChanged()
        {
            supportedFiles = Files.OfType<IBitmapFileInfo>().Where(file => file.IsMetadataSupported).ToList();

            IsEnabled = supportedFiles.Count == Files.Count;

            updateRunner.RunAndCancelPrevious(Update);
        }

        private async Task Update(CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                var metadata = await Task.WhenAll(supportedFiles.Select(metadataService.GetMetadataAsync));

                if (cancellationToken.IsCancellationRequested) return;

                TitleTextboxModel.SetValues(metadata.Select(m => m.Get(MetadataProperties.Title).Trim()).ToList());
                KeywordsSectionModel.SetValues(supportedFiles, metadata.Select(m => m.Get(MetadataProperties.Keywords)).ToList());
                AuthorTextboxModel.SetValues(metadata.Select(m => string.Join("; ", m.Get(MetadataProperties.Author))).ToList());
                CopyrightTextboxModel.SetValues(metadata.Select(m => m.Get(MetadataProperties.Copyright).Trim()).ToList());
            }
            else
            {
                TitleTextboxModel.Clear();
                KeywordsSectionModel.Clear();
                AuthorTextboxModel.Clear();
                CopyrightTextboxModel.Clear();
            }
        }


        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

        [RelayCommand]
        private void ToggleTagPeopleOnPhoto()
        {
            messenger.Publish(new SetTagPeopleToolActive(!IsTagPeopleOnPhotoButtonChecked));
        }

        private async Task WriteMetadataAsync<T>(IMetadataProperty<T> property, T value)
        {
            await writeFilesRunner.Enqueue(async () =>
            {
                foreach (var file in supportedFiles)
                {
                    await metadataService.WriteMetadataAsync(file, property, value);
                }
            });
        }

    }
}
