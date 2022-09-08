using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Messages;
using System.ComponentModel;

namespace PhotoViewerCore.ViewModels
{
    public delegate IMetadataPanelModel MetadataPanelModelFactory(bool tagPeopleOnPhotoButtonVisible);

    public interface IMetadataPanelModel : INotifyPropertyChanged { }

    public partial class MetadataPanelModel : ViewModelBase, IMetadataPanelModel
    {
        [ObservableProperty]
        private bool isVisible;

        public bool IsTagPeopleOnPhotoButtonVisible { get; }

        [ObservableProperty]
        private bool isTagPeopleOnPhotoButtonChecked;

        private readonly IMessenger messenger;

        public MetadataPanelModel(IMessenger messenger, bool tagPeopleOnPhotoButtonVisible)
        {
            this.messenger = messenger;

            IsTagPeopleOnPhotoButtonVisible = tagPeopleOnPhotoButtonVisible;

            if (IsTagPeopleOnPhotoButtonVisible)
            {
                messenger.Subscribe<TagPeopleToolActiveChanged>(msg =>
                {
                    IsTagPeopleOnPhotoButtonChecked = msg.IsVisible;
                });
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
