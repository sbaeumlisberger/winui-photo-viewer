using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Models;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.ViewModels;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Diagnostics;
using PhotoViewer.App.Utils.Logging;

namespace PhotoViewer.Core;

public class ViewModelFactory
{
    public static ViewModelFactory Instance { get; private set; } = null!; 
 
    private readonly IMessenger messenger;
    private readonly ApplicationSession applicationSession;
    private readonly IMediaFilesLoaderService mediaFilesLoaderService = new MediaFilesLoaderService();
    private readonly IMetadataService metadataService = new MetadataService();
    private readonly IDeleteMediaService deleteMediaService = new DeleteMediaService();
    private readonly IRotateBitmapService rotateBitmapService;
    private readonly IImageLoaderService imageLoaderService = new ImageLoaderService(new GifImageLoaderService());
    private readonly IDisplayRequestService displayRequestService = new DisplayRequestService();
    private readonly ILocationService locationService = new LocationService();
    private readonly ISettingsService settingService = new SettingsService();
    private readonly IPersonalizationService personalizationService = new PersonalizationService();
    private readonly IClipboardService clipboardService = new ClipboardService();
    private readonly IGpxService gpxService = new GpxService();
    private readonly ISuggestionsService peopleSuggestionsService = new SuggestionsService("people");
    private readonly ISuggestionsService keywordsSuggestionsService = new SuggestionsService("keywords");
    private readonly IDialogService dialogService;
       
    private ViewModelFactory(IMessenger messenger, IDialogService dialogService)
    {
        this.messenger = messenger;
        this.dialogService = dialogService;
        applicationSession = new ApplicationSession(messenger);
        rotateBitmapService = new RotateBitmapService(metadataService);
    }

    public static void Initialize(IMessenger messenger, IDialogService dialogService)
    {
        Instance = new ViewModelFactory(messenger, dialogService);
    }

    public FlipViewPageModel CreateFlipViewPageModel()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var settings = ApplicationSettingsProvider.GetSettings();
        var deleteFilesCommand = CreateDeleteFilesCommand(settings);
        var flipViewPageModel = new FlipViewPageModel(
            applicationSession,
            messenger,
            () => new MediaFlipViewModel(
                messenger,
                dialogService,
                mediaFilesLoaderService,
                (mediaFile) => CreateMediaFlipViewItemModel(mediaFile, deleteFilesCommand),
                settings),
            () => new DetailsBarModel(messenger, metadataService, settings),
            (flipViewPageModel) => new FlipViewPageCommandBarModel(
                messenger,
                dialogService,
                mediaFilesLoaderService,
                rotateBitmapService,
                flipViewPageModel.SelectPreviousCommand,
                flipViewPageModel.SelectNextCommand,
                settings,
                deleteFilesCommand,
                new MoveRawFilesToSubfolderCommand(applicationSession, settings, dialogService),
                new ShiftDatenTakenCommand(applicationSession, messenger, dialogService, metadataService)),
            displayRequestService,
            showTagPeopleOnPhotoButton => new MetadataPanelModel(
                messenger,
                metadataService,
                locationService,
                dialogService,
                clipboardService,
                peopleSuggestionsService,
                keywordsSuggestionsService,
                gpxService,
                settings,
                showTagPeopleOnPhotoButton));
        sw.Stop();
        Log.Info($"CreateFlipViewPageModel took {sw.ElapsedMilliseconds}ms");
        return flipViewPageModel;
    }

    public OverviewPageModel CreateOverviewPageModel()
    {
        var settings = ApplicationSettingsProvider.GetSettings();
        var deleteFilesCommand = CreateDeleteFilesCommand(settings);
        var mediaFileContextFlyoutModel = new MediaFileContextMenuModel(
            messenger,
            metadataService,
            personalizationService,
            rotateBitmapService,
            dialogService,
            clipboardService,
            deleteFilesCommand);
        return new OverviewPageModel(
            applicationSession,
            messenger,
            new OverviewPageCommandBarModel(
                messenger,
                dialogService,
                mediaFilesLoaderService,
                deleteFilesCommand,
                rotateBitmapService,
                settings),
            mediaFileContextFlyoutModel,
            showTagPeopleOnPhotoButton => new MetadataPanelModel(
                messenger,
                metadataService,
                locationService,
                dialogService,
                clipboardService,
                peopleSuggestionsService,
                keywordsSuggestionsService,
                gpxService,
                settings,
                showTagPeopleOnPhotoButton));
    }

    public SettingsPageModel CreateSettingsPageModel()
    {
        var settings = ApplicationSettingsProvider.GetSettings();
        return new SettingsPageModel(messenger, settings, settingService, dialogService);
    }

    private IDeleteFilesCommand CreateDeleteFilesCommand(ApplicationSettings settings)
    {
        return new DeleteFilesCommand(messenger, deleteMediaService, dialogService, settingService, settings);
    }

    private IMediaFlipViewItemModel CreateMediaFlipViewItemModel(IMediaFileInfo mediaFile, IDeleteFilesCommand deleteFilesCommand)
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
            IBitmapFileInfo bitmapFile => new BitmapFlipViewItemModel(
                bitmapFile,
                mediaFileContextFlyoutModel,
                (bitmapFile) => new TagPeopleToolModel(bitmapFile, messenger, peopleSuggestionsService, metadataService, dialogService),
                messenger,
                imageLoaderService),
            IVideoFileInfo => new VideoFlipViewItemModel(mediaFile, mediaFileContextFlyoutModel, messenger),
            IVectorGraphicFileInfo => new VectorGraphicFlipViewItemModel(mediaFile),
            _ => throw new Exception($"Unexcpected type of media file: {mediaFile.GetType()}")
        };
    }
}
