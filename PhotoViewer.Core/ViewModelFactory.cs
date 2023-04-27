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
using System.Windows.Input;

namespace PhotoViewer.Core;

public interface IViewModelFactory
{
    IDetailsBarModel CreateDetailsBarModel();
    IMediaFlipViewModel CreateMediaFlipViewModel();
    IFlipViewPageCommandBarModel CreateFlipViewPageCommandBarModel(ICommand selectPreviousCommand, ICommand selectNextCommand);
    IMetadataPanelModel CreateMetadataPanelModel(bool showTagPeopleOnPhotoButton);
    ITagPeopleToolModel CreateTagPeopleToolModel(IBitmapFileInfo bitmapFile);
    IOverviewItemModel CreateOverviewItemModel(IMediaFileInfo mediaFile);
    IOverviewPageCommandBarModel CreateOverviewPageCommandBarModel();
    IMediaFileContextMenuModel CreateMediaFileContextMenuModel();
    ICompareViewModel CreateCompareViewModel(IObservableList<IBitmapFileInfo> bitmapFiles);
    ICropImageToolModel CreateCropImageToolModel(IBitmapFileInfo bitmapFile);
}

public class ViewModelFactory : IViewModelFactory
{
    public static ViewModelFactory Instance { get; private set; } = null!;

    private readonly IMessenger messenger;
    private readonly ApplicationSettings settings;
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
    private readonly IFaceDetectionService faceDetectionService = new FaceDetectionService();
    private readonly ICropImageService cropImageService;

    private ViewModelFactory(IMessenger messenger, ApplicationSettings settings, IDialogService dialogService)
    {
        this.messenger = messenger;
        this.settings = settings;
        this.dialogService = dialogService;
        applicationSession = new ApplicationSession(messenger);
        rotateBitmapService = new RotateBitmapService(metadataService);
        cropImageService = new CropImageService(messenger, metadataService);
    }

    public static void Initialize(IMessenger messenger, ApplicationSettings settings, IDialogService dialogService)
    {
        Instance = new ViewModelFactory(messenger, settings, dialogService);
    }

    public FlipViewPageModel CreateFlipViewPageModel()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var flipViewPageModel = new FlipViewPageModel(applicationSession, messenger, this, displayRequestService);
        sw.Stop();
        Log.Info($"CreateFlipViewPageModel took {sw.ElapsedMilliseconds}ms");
        return flipViewPageModel;
    }

    public OverviewPageModel CreateOverviewPageModel()
    {
        return new OverviewPageModel(applicationSession, messenger, this);
    }

    public SettingsPageModel CreateSettingsPageModel()
    {
        return new SettingsPageModel(messenger, settings, settingService, dialogService);
    }

    public ITagPeopleToolModel CreateTagPeopleToolModel(IBitmapFileInfo bitmapFile)
    {
        return new TagPeopleToolModel(
            bitmapFile,
            messenger,
            peopleSuggestionsService,
            metadataService,
            dialogService,
            faceDetectionService);
    }

    public IDetailsBarModel CreateDetailsBarModel()
    {
        return new DetailsBarModel(messenger, metadataService, settings);
    }

    public IMediaFlipViewModel CreateMediaFlipViewModel()
    {
        var deleteFilesCommand = CreateDeleteFilesCommand(); // TODO cache?
        return new MediaFlipViewModel(
              messenger,
              dialogService,
              mediaFilesLoaderService,
              (mediaFile) => CreateMediaFlipViewItemModel(mediaFile, deleteFilesCommand),
              settings);
    }

    public IFlipViewPageCommandBarModel CreateFlipViewPageCommandBarModel(ICommand selectPreviousCommand, ICommand selectNextCommand)
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return new FlipViewPageCommandBarModel(
            messenger,
            dialogService,
            mediaFilesLoaderService,
            rotateBitmapService,
            selectPreviousCommand,
            selectNextCommand,
            settings,
            deleteFilesCommand,
            new MoveRawFilesToSubfolderCommand(applicationSession, settings, dialogService),
            new DeleteSingleRawFilesCommand(applicationSession, dialogService),
            new ShiftDatenTakenCommand(applicationSession, messenger, dialogService, metadataService),
            new ImportGpxTrackCommand(applicationSession, messenger, dialogService, metadataService, gpxService));
    }

    public IMetadataPanelModel CreateMetadataPanelModel(bool showTagPeopleOnPhotoButton)
    {
        return new MetadataPanelModel(
               messenger,
               metadataService,
               locationService,
               dialogService,
               clipboardService,
               peopleSuggestionsService,
               keywordsSuggestionsService,
               gpxService,
               settings,
               showTagPeopleOnPhotoButton);
    }

    public IOverviewItemModel CreateOverviewItemModel(IMediaFileInfo mediaFile)
    {
        return new OverviewItemModel(mediaFile, messenger, metadataService);
    }

    public IOverviewPageCommandBarModel CreateOverviewPageCommandBarModel()
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return new OverviewPageCommandBarModel(messenger, dialogService, mediaFilesLoaderService, deleteFilesCommand, rotateBitmapService, settings);
    }

    public IMediaFileContextMenuModel CreateMediaFileContextMenuModel()
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return CreateMediaFileContextMenuModel(deleteFilesCommand);
    }

    private IDeleteFilesCommand CreateDeleteFilesCommand()
    {
        return new DeleteFilesCommand(messenger, deleteMediaService, dialogService, settingService, settings);
    }

    private IMediaFlipViewItemModel CreateMediaFlipViewItemModel(IMediaFileInfo mediaFile, IDeleteFilesCommand deleteFilesCommand)
    {
        var mediaFileContextFlyoutModel = CreateMediaFileContextMenuModel(deleteFilesCommand);
        return mediaFile switch
        {
            IBitmapFileInfo bitmapFile => CreateBitmapFlipViewItemModel(bitmapFile, mediaFileContextFlyoutModel),
            IVideoFileInfo => new VideoFlipViewItemModel(mediaFile, mediaFileContextFlyoutModel, messenger),
            IVectorGraphicFileInfo => new VectorGraphicFlipViewItemModel(mediaFile, mediaFileContextFlyoutModel),
            _ => throw new Exception($"Unexcpected type of media file: {mediaFile.GetType()}")
        };
    }

    private IMediaFileContextMenuModel CreateMediaFileContextMenuModel(IDeleteFilesCommand deleteFilesCommand)
    {
        return new MediaFileContextMenuModel(
            messenger,
            metadataService,
            personalizationService,
            rotateBitmapService,
            dialogService,
            clipboardService,
            deleteFilesCommand);
    }

    private BitmapFlipViewItemModel CreateBitmapFlipViewItemModel(IBitmapFileInfo bitmapFile, IMediaFileContextMenuModel mediaFileContextFlyoutModel)
    {
        return new BitmapFlipViewItemModel(
            bitmapFile,
            mediaFileContextFlyoutModel,
            this,
            messenger,
            imageLoaderService);
    }

    public ComparePageModel CreateComparePageModel()
    { 
        return new ComparePageModel(applicationSession, messenger, this);
    }

    public ICompareViewModel CreateCompareViewModel(IObservableList<IBitmapFileInfo> bitmapFiles)
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return new CompareViewModel(bitmapFiles, settings, imageLoaderService, deleteFilesCommand);
    }

    public ICropImageToolModel CreateCropImageToolModel(IBitmapFileInfo bitmapFile)
    {
        return new CropImageToolModel(bitmapFile, messenger, cropImageService, dialogService);
    }
}
