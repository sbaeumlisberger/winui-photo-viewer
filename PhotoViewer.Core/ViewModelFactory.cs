using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.ViewModels;
using PhotoViewer.Core.ViewModels.Shared;
using System.Diagnostics;
using System.Windows.Input;

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
    EditImageOverlayModel CreateEditImageOverlayModel();
    SortMenuModel CreateSortMenuModel();
    ToolsMenuModel CreateToolsMenuModel();
}

public class ViewModelFactory : IViewModelFactory
{
    private readonly IMessenger messenger;
    private readonly ApplicationSettings settings;
    private readonly ApplicationSession applicationSession;
    private readonly IMediaFilesLoaderService mediaFilesLoaderService;
    private readonly IMetadataService metadataService = new MetadataService();
    private readonly IRotateBitmapService rotateBitmapService;
    private readonly IImageLoaderService imageLoaderService = new ImageLoaderService(new GifImageLoaderService());
    private readonly ICachedImageLoaderService cachedImageLoaderService = CachedImageLoaderService.Instance;
    private readonly IDisplayRequestService displayRequestService = new DisplayRequestService();
    private readonly ILocationService locationService = new CachedLocationService(new LocationService());
    private readonly ISettingsService settingService = new SettingsService();
    private readonly IPersonalizationService personalizationService = new PersonalizationService();
    private readonly IClipboardService clipboardService = new ClipboardService();
    private readonly IGpxService gpxService;
    private readonly ISuggestionsService peopleSuggestionsService = new SuggestionsService("people");
    private readonly ISuggestionsService keywordsSuggestionsService = new SuggestionsService("keywords");
    private readonly IDialogService dialogService = new DialogService();
    private readonly IFaceDetectionService faceDetectionService = new FaceDetectionService();
    private readonly ICropImageService cropImageService;
    private readonly IBackgroundTaskService backgroundTaskService = new BackgroundTaskService();
    private readonly SortService sortService = new SortService(new MetadataService());
    private readonly IDeleteFilesService deleteFilesService;
    private readonly IFileSystemService fileSystemService = new FileSystemService();

    public ViewModelFactory(ApplicationSettings settings, IMessenger messenger, IMediaFilesLoaderService mediaFilesLoaderService)
    {
        this.messenger = messenger;
        this.settings = settings;
        this.mediaFilesLoaderService = mediaFilesLoaderService;
        gpxService = new GpxService(metadataService);
        applicationSession = new ApplicationSession(messenger);
        rotateBitmapService = new RotateBitmapService(metadataService);
        cropImageService = new CropImageService(messenger, metadataService);
        deleteFilesService = new DeleteFilesService(messenger, dialogService, settingService, backgroundTaskService, settings);
    }

    public MainWindowModel CreateMainWindowModel()
    {
        return new MainWindowModel(settings, messenger, backgroundTaskService, dialogService);
    }

    public FlipViewPageModel CreateFlipViewPageModel()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var flipViewPageModel = new FlipViewPageModel(settings, applicationSession, messenger, this, displayRequestService);
        sw.Stop();
        Log.Debug($"CreateFlipViewPageModel took {sw.ElapsedMilliseconds}ms");
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
              fileSystemService,
              CreateMediaFlipViewItemModel,
              settings);
    }

    public IFlipViewPageCommandBarModel CreateFlipViewPageCommandBarModel(ICommand selectPreviousCommand, ICommand selectNextCommand)
    {
        return new FlipViewPageCommandBarModel(
            messenger,
            dialogService,
            mediaFilesLoaderService,
            rotateBitmapService,
            this,
            selectPreviousCommand,
            selectNextCommand,
            settings,
            deleteFilesService);
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
        return new OverviewPageCommandBarModel(
            messenger,
            dialogService,
            mediaFilesLoaderService,
            deleteFilesService,
            rotateBitmapService,
            this,
            settings);
    }

    public IMediaFileContextMenuModel CreateMediaFileContextMenuModel(bool isRenameFilesEnabled = false)
    {
        return new MediaFileContextMenuModel(
            messenger,
            metadataService,
            personalizationService,
            rotateBitmapService,
            dialogService,
            clipboardService,
            deleteFilesService,
            isRenameFilesEnabled);
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
        return new CompareViewModel(bitmapFiles, settings, deleteFilesService, this);
    }

    public ICropImageToolModel CreateCropImageToolModel(IBitmapFileInfo bitmapFile)
    {
        return new CropImageToolModel(bitmapFile, messenger, cropImageService, dialogService);
    }

    public IImageViewModel CreateImageViewModel(IBitmapFileInfo bitmapFile)
    {
        return new ImageViewModel(bitmapFile, cachedImageLoaderService, messenger);
    }

    public EditLocationDialogModel CreateEditLocationDialogModel(Location? orginalLocation, Func<Location?, Task> saveLocation)
    {
        return new EditLocationDialogModel(orginalLocation, saveLocation, locationService, clipboardService);
    }

    public BackgroundTasksViewModel CreateBackgroundTasksViewModel()
    {
        return new BackgroundTasksViewModel(backgroundTaskService);
    }

    public EditImageOverlayModel CreateEditImageOverlayModel()
    {
        return new EditImageOverlayModel(messenger, dialogService, metadataService);
    }

    public PeopleTaggingPageModel CreatePeopleTaggingBatchViewPageModel()
    {
        return new PeopleTaggingPageModel(
            applicationSession,
            messenger,
            faceDetectionService,
            imageLoaderService,
            peopleSuggestionsService,
            metadataService);
    }

    public SortMenuModel CreateSortMenuModel()
    {
        return new SortMenuModel(applicationSession, sortService, messenger);
    }

    public ToolsMenuModel CreateToolsMenuModel()
    {
        return new ToolsMenuModel(
            new MoveRawFilesToSubfolderCommand(applicationSession, settings, dialogService),
            new DeleteSingleRawFilesCommand(applicationSession, messenger, dialogService),
            new ShiftDatenTakenCommand(applicationSession, messenger, dialogService, metadataService),
            new ImportGpxTrackCommand(applicationSession, messenger, dialogService, metadataService, gpxService),
            new PrefixFilesByDateCommand(applicationSession, dialogService, metadataService, messenger));
    }
}
