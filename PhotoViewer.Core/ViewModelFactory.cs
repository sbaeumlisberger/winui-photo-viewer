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
using PhotoViewer.Core.ViewModels.Shared;

namespace PhotoViewer.Core;

public interface IViewModelFactory
{
    MainWindowModel CreateMainWindowModel();
    IDetailsBarModel CreateDetailsBarModel();
    IMediaFlipViewModel CreateMediaFlipViewModel();
    IFlipViewPageCommandBarModel CreateFlipViewPageCommandBarModel(ICommand selectPreviousCommand, ICommand selectNextCommand);
    IMetadataPanelModel CreateMetadataPanelModel(bool showTagPeopleOnPhotoButton);
    ITagPeopleToolModel CreateTagPeopleToolModel(IBitmapFileInfo bitmapFile);
    IOverviewItemModel CreateOverviewItemModel(IMediaFileInfo mediaFile);
    IOverviewPageCommandBarModel CreateOverviewPageCommandBarModel();
    IMediaFileContextMenuModel CreateMediaFileContextMenuModel(bool isRenameFilesEnabled = false);
    ICompareViewModel CreateCompareViewModel(IObservableList<IBitmapFileInfo> bitmapFiles);
    ICropImageToolModel CreateCropImageToolModel(IBitmapFileInfo bitmapFile);
    IImageViewModel CreateImageViewModel(IBitmapFileInfo bitmapFile);
    EditLocationDialogModel CreateEditLocationDialogModel(Location? orginalLocation, Func<Location?, Task> saveLocation);
    BackgroundTasksViewModel CreateBackgroundTasksViewModel();
}

public class ViewModelFactory : IViewModelFactory
{
    public static ViewModelFactory Instance { get; private set; } = null!;

    private readonly IMessenger messenger;
    private readonly ApplicationSettings settings;
    private readonly ApplicationSession applicationSession;
    private readonly IMediaFilesLoaderService mediaFilesLoaderService = new MediaFilesLoaderService();
    private readonly IMetadataService metadataService = new MetadataService();
    private readonly IDeleteMediaFilesService deleteMediaService = new DeleteMediaFilesService();
    private readonly IRotateBitmapService rotateBitmapService;
    private readonly ICachedImageLoaderService imageLoaderService = CachedImageLoaderService.Instance;
    private readonly IDisplayRequestService displayRequestService = new DisplayRequestService();
    private readonly ILocationService locationService = new LocationService();
    private readonly ISettingsService settingService = new SettingsService();
    private readonly IPersonalizationService personalizationService = new PersonalizationService();
    private readonly IClipboardService clipboardService = new ClipboardService();
    private readonly IGpxService gpxService;
    private readonly ISuggestionsService peopleSuggestionsService = new SuggestionsService("people");
    private readonly ISuggestionsService keywordsSuggestionsService = new SuggestionsService("keywords");
    private readonly IDialogService dialogService;
    private readonly IFaceDetectionService faceDetectionService = new FaceDetectionService();
    private readonly ICropImageService cropImageService;
    private readonly IBackgroundTaskService backgroundTaskService = new BackgroundTaskService();

    private ViewModelFactory(IMessenger messenger, ApplicationSettings settings, IDialogService dialogService)
    {
        this.messenger = messenger;
        this.settings = settings;
        this.dialogService = dialogService;
        gpxService = new GpxService(metadataService);
        applicationSession = new ApplicationSession(messenger);
        rotateBitmapService = new RotateBitmapService(metadataService);
        cropImageService = new CropImageService(messenger, metadataService);
    }

    public static void Initialize(IMessenger messenger, ApplicationSettings settings, IDialogService dialogService)
    {
        Instance = new ViewModelFactory(messenger, settings, dialogService);
    }

    public MainWindowModel CreateMainWindowModel() 
    {
        return new MainWindowModel(messenger, backgroundTaskService, dialogService);
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
        return new OverviewPageModel(applicationSession, messenger, this, dialogService);
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
        return new MediaFlipViewModel(
              messenger,
              dialogService,
              mediaFilesLoaderService,
              CreateMediaFlipViewItemModel,
              settings);
    }

    public IFlipViewPageCommandBarModel CreateFlipViewPageCommandBarModel(
        ICommand selectPreviousCommand, ICommand selectNextCommand)
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return new FlipViewPageCommandBarModel(
            messenger,
            dialogService,
            mediaFilesLoaderService,
            rotateBitmapService,
            this,
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
               this,
               peopleSuggestionsService,
               keywordsSuggestionsService,
               gpxService,
               backgroundTaskService,
               settings,
               showTagPeopleOnPhotoButton);
    }

    public IOverviewItemModel CreateOverviewItemModel(IMediaFileInfo mediaFile)
    {
        return new OverviewItemModel(mediaFile, messenger, metadataService, dialogService);
    }

    public IOverviewPageCommandBarModel CreateOverviewPageCommandBarModel()
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return new OverviewPageCommandBarModel(
            messenger,
            dialogService,
            mediaFilesLoaderService,
            deleteFilesCommand,
            rotateBitmapService,
            this,
            settings);
    }

    public IMediaFileContextMenuModel CreateMediaFileContextMenuModel(bool isRenameFilesEnabled = false)
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return CreateMediaFileContextMenuModel(deleteFilesCommand, isRenameFilesEnabled);
    }

    private IDeleteFilesCommand CreateDeleteFilesCommand()
    {
        return new DeleteFilesCommand(
            messenger, 
            deleteMediaService,
            dialogService,
            settingService, 
            backgroundTaskService, 
            settings);
    }

    private IMediaFlipViewItemModel CreateMediaFlipViewItemModel(IMediaFileInfo mediaFile)
    {
        return mediaFile switch
        {
            IBitmapFileInfo bitmapFile => CreateBitmapFlipViewItemModel(bitmapFile),
            IVideoFileInfo => new VideoFlipViewItemModel(mediaFile, this, messenger),
            IVectorGraphicFileInfo => new VectorGraphicFlipViewItemModel(mediaFile, this),
            _ => throw new Exception($"Unexcpected type of media file: {mediaFile.GetType()}")
        };
    }

    private IMediaFileContextMenuModel CreateMediaFileContextMenuModel(
        IDeleteFilesCommand deleteFilesCommand, bool isRenameFilesEnabled = false)
    {
        return new MediaFileContextMenuModel(
            messenger,
            metadataService,
            personalizationService,
            rotateBitmapService,
            dialogService,
            clipboardService,
            deleteFilesCommand,
            isRenameFilesEnabled);
    }

    private BitmapFlipViewItemModel CreateBitmapFlipViewItemModel(IBitmapFileInfo bitmapFile)
    {
        return new BitmapFlipViewItemModel(bitmapFile, this, messenger);
    }

    public ComparePageModel CreateComparePageModel()
    {
        return new ComparePageModel(applicationSession, messenger, this);
    }

    public ICompareViewModel CreateCompareViewModel(IObservableList<IBitmapFileInfo> bitmapFiles)
    {
        var deleteFilesCommand = CreateDeleteFilesCommand();
        return new CompareViewModel(bitmapFiles, settings, deleteFilesCommand, this);
    }

    public ICropImageToolModel CreateCropImageToolModel(IBitmapFileInfo bitmapFile)
    {
        return new CropImageToolModel(bitmapFile, messenger, cropImageService, dialogService);
    }

    public IImageViewModel CreateImageViewModel(IBitmapFileInfo bitmapFile)
    {
        return new ImageViewModel(bitmapFile, imageLoaderService, messenger);
    }

    public EditLocationDialogModel CreateEditLocationDialogModel(Location? orginalLocation, Func<Location?, Task> saveLocation)
    {
        return new EditLocationDialogModel(orginalLocation, saveLocation, locationService, clipboardService);
    }

    public BackgroundTasksViewModel CreateBackgroundTasksViewModel()
    {
        return new BackgroundTasksViewModel(backgroundTaskService);
    }
}
