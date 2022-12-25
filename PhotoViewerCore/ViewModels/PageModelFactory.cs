using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerCore.Commands;
using PhotoViewerCore.Services;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;

namespace PhotoViewerApp.ViewModels;

public class PageModelFactory
{
    private static readonly IMessenger messenger = StrongReferenceMessenger.Default;
    private static readonly Session session = new Session(messenger);
    private static readonly IMediaFilesLoaderService mediaFilesLoaderService = new MediaFilesLoaderService();
    private static readonly IMetadataService metadataService = new MetadataService();
    private static readonly IDeleteMediaService deleteMediaService = new DeleteMediaService();
    private static readonly IRotateBitmapService rotateBitmapService = new RotateBitmapService(metadataService);
    private static readonly IImageLoaderService imageLoaderService = new ImageLoaderService(new GifImageLoaderService());
    private static readonly IDisplayRequestService displayRequestService = new DisplayRequestService();
    private static readonly ILocationService locationService = new LocationService();
    private static readonly ISettingsService settingService = new SettingsService();
    private static readonly IPersonalizationService personalizationService = new PersonalizationService();
    private static readonly IClipboardService clipboardService = new ClipboardService();

    public static FlipViewPageModel CreateFlipViewPageModel(IDialogService dialogService)
    {
        var settings = ApplicationSettingsProvider.GetSettings();
        var deleteFilesCommand = new DeleteFilesCommand(
            messenger, 
            deleteMediaService,
            dialogService,
            settingService, 
            settings);
        return new FlipViewPageModel(
            session,
            messenger,
            () => new MediaFlipViewModel(messenger, dialogService, mediaFilesLoaderService,
                (mediaFile) =>
                {
                    var mediaFileContextFlyoutModel = new MediaFileContextMenuModel(
                        messenger, 
                        metadataService, 
                        personalizationService,
                        rotateBitmapService, 
                        dialogService, 
                        clipboardService, 
                        deleteFilesCommand);
                    return mediaFile switch
                    {
                        IBitmapFileInfo => new BitmapFlipViewItemModel(mediaFile, mediaFileContextFlyoutModel, messenger, imageLoaderService),
                        IVideoFileInfo => new VideoFlipViewItemModel(mediaFile, mediaFileContextFlyoutModel, messenger),
                        IVectorGraphicFileInfo => new VectorGraphicFlipViewItemModel(mediaFile),
                        _ => throw new Exception($"Unexcpected type of media file: {mediaFile.GetType()}")
                    };
                },
                settings),
            () => new DetailsBarModel(messenger, metadataService),
            (flipViewPageModel) => new FlipViewPageCommandBarModel(
                session, 
                messenger,
                dialogService,
                mediaFilesLoaderService, 
                rotateBitmapService,
                flipViewPageModel, 
                deleteMediaService, 
                settings, 
                deleteFilesCommand),
            displayRequestService,
            showTagPeopleOnPhotoButton => new MetadataPanelModel(
                messenger,
                metadataService,
                locationService,
                dialogService, 
                clipboardService,
                new SuggestionsService("people"),
                new SuggestionsService("keywords"),
                showTagPeopleOnPhotoButton));
    }

    public static OverviewPageModel CreateOverviewPageModel(IDialogService dialogService)
    {
        var settings = ApplicationSettingsProvider.GetSettings();
        var deleteFilesCommand = new DeleteFilesCommand(
            messenger, 
            deleteMediaService,
            dialogService, 
            settingService,
            settings);
        var mediaFileContextFlyoutModel = new MediaFileContextMenuModel(
            messenger, 
            metadataService, 
            personalizationService,
            rotateBitmapService, 
            dialogService,
            clipboardService,
            deleteFilesCommand);
        return new OverviewPageModel(
            session,
            messenger,
            dialogService,
            new OverviewPageCommandBarModel(
                messenger, dialogService, 
                mediaFilesLoaderService, 
                deleteFilesCommand, 
                settings),
            mediaFileContextFlyoutModel,
            showTagPeopleOnPhotoButton => new MetadataPanelModel(
                messenger, 
                metadataService,
                locationService, 
                dialogService, 
                clipboardService,
                new SuggestionsService("people"),
                new SuggestionsService("keywords"),           
                showTagPeopleOnPhotoButton));
    }

    public static SettingsPageModel CreateSettingsPageModel(IDialogService dialogService)
    {
        var settings = ApplicationSettingsProvider.GetSettings();
        return new SettingsPageModel(messenger, settings, settingService, dialogService);
    }
}
