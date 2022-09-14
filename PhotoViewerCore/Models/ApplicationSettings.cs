using CommunityToolkit.Mvvm.ComponentModel;

namespace PhotoViewerCore.Models;

[ObservableObject]
 public partial class ApplicationSettings
{
    [ObservableProperty] private bool showDeleteAnimation = true;
    [ObservableProperty] private bool autoOpenMetadataPanel = false;
    [ObservableProperty] private bool autoOpenDetailsBar = false;
    [ObservableProperty] private TimeSpan diashowTime = TimeSpan.FromSeconds(3);

    [ObservableProperty] private bool linkRawFiles = true;
    [ObservableProperty] private string rawFilesFolderName = "RAWs";
    [ObservableProperty] private DeleteLinkedFilesOption deleteLinkedFilesOption = DeleteLinkedFilesOption.Ask;

    [ObservableProperty] private bool includeVideos = true;
}