using CommunityToolkit.Mvvm.ComponentModel;
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
    }

    public partial class MetadataPanelModel : ViewModelBase, IMetadataPanelModel
    {
        [ObservableProperty]
        private bool isVisible = true;

        [ObservableProperty]
        private bool isEnabled = false;

        public MetadataStringViewModel TitleModel { get; } = new MetadataStringViewModel();

        [ObservableProperty]
        private IList<IMediaFileInfo> files = Array.Empty<IMediaFileInfo>();

        private IList<IBitmapFileInfo> supportedFiles = Array.Empty<IBitmapFileInfo>();

        public bool IsTagPeopleOnPhotoButtonVisible { get; }

        [ObservableProperty]
        private bool isTagPeopleOnPhotoButtonChecked;

        private readonly IMessenger messenger;

        private readonly IMetadataService metadataService;

        private readonly CancelableTaskRunner cancelableTaskRunner = new CancelableTaskRunner();

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
        }

        partial void OnFilesChanged(IList<IMediaFileInfo> value)
        {
            supportedFiles = Files.OfType<IBitmapFileInfo>().Where(file => file.IsMetadataSupported).ToList();
            
            IsEnabled = supportedFiles.Count == Files.Count;

            cancelableTaskRunner.RunAndCancelPrevious(Update);
        }

        private async Task Update(CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                var metadata = await Task.WhenAll(supportedFiles.Select(metadataService.GetMetadataAsync));

                if (cancellationToken.IsCancellationRequested) return;

                TitleModel.SetValues(metadata.Select(m => m.Get(MetadataProperties.Title).Trim()));
            }
            else 
            {
                TitleModel.Clear();
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

    }
}
