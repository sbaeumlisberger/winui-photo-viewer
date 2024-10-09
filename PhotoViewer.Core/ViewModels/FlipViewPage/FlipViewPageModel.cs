using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.ComponentModel;

namespace PhotoViewer.Core.ViewModels;

public partial class FlipViewPageModel : ViewModelBase
{
    public IDetailsBarModel DetailsBarModel => detailsBarModel ?? CreateDetailsBarModel();
    private IDetailsBarModel? detailsBarModel;

    public IFlipViewPageCommandBarModel CommandBarModel => commandBarModel ?? CreateCommandBarModel();
    private IFlipViewPageCommandBarModel? commandBarModel;

    public IMediaFlipViewModel FlipViewModel { get; }

    public IMetadataPanelModel MetadataPanelModel => metadataPanelModel ?? CreateMetadataPanelModel();
    private IMetadataPanelModel? metadataPanelModel;

    public EditImageOverlayModel EditImageOverlayModel => editImageOverlayModel ?? CreateEditImageOverlayModel();
    private EditImageOverlayModel? editImageOverlayModel;

    public bool ShowUI { get; private set; } = true;

    public bool ShowDetailsBarOnStartup { get; private set; } = false;

    public bool ShowMetadataPanelOnStartup { get; private set; } = false;


    private readonly ApplicationSession session;

    private readonly IDisplayRequestService displayRequestService;

    private readonly IViewModelFactory viewModelFactory;

    private IDisposable? displayRequest;

    internal FlipViewPageModel(
        ApplicationSettings settings,
        ApplicationSession session,
        IMessenger messenger,
        IViewModelFactory viewModelFactory,
        IDisplayRequestService displayRequestService) : base(messenger)
    {
        this.session = session;
        this.displayRequestService = displayRequestService;
        this.viewModelFactory = viewModelFactory;

        ShowDetailsBarOnStartup = settings.AutoOpenDetailsBar;
        ShowMetadataPanelOnStartup = settings.AutoOpenMetadataPanel;

        FlipViewModel = viewModelFactory.CreateMediaFlipViewModel();
        FlipViewModel.PropertyChanged += FlipViewModel_PropertyChanged;

        Messenger.Register<StartDiashowMessage>(this, OnStartDiashowMessageReceived);
        Messenger.Register<ExitDiashowMessage>(this, OnExitDiashowMessageReceived);
    }

    protected override void OnCleanup()
    {
        FlipViewModel.PropertyChanged -= FlipViewModel_PropertyChanged;
        displayRequest.DisposeSafely(() => displayRequest = null);

        FlipViewModel.Cleanup();
        DetailsBarModel.Cleanup();
        CommandBarModel.Cleanup();
        MetadataPanelModel.Cleanup();
    }

    public void OnNavigatedTo(object navigationParameter, bool popNavigationState)
    {
        if (session.Files.Any())
        {
            IMediaFileInfo? selectedItem = null;

            if (popNavigationState)
            {
                if (Messenger.Send<PopNavigationStateMessage>().Response is Dictionary<string, object> navigationState)
                {
                    var oldSelectedItem = (IMediaFileInfo?)navigationState[nameof(FlipViewModel.SelectedItem)];
                    selectedItem = session.Files.Contains(oldSelectedItem) ? oldSelectedItem : null;
                }
            }
            else
            {
                selectedItem = (IMediaFileInfo?)navigationParameter;
            }

            FlipViewModel.SetFiles(session.Files, selectedItem);
        }
    }

    public void OnNavigatedFrom()
    {
        var navigationState = new Dictionary<string, object?>();
        navigationState[nameof(FlipViewModel.SelectedItem)] = FlipViewModel.SelectedItem;
        Messenger.Send(new PushNavigationStateMessage(navigationState));
    }

    private void OnStartDiashowMessageReceived(StartDiashowMessage msg)
    {
        ShowUI = false;
        Messenger.Send(new EnterFullscreenMessage());
        try
        {
            displayRequest = displayRequestService.RequestActive();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to request display active", ex);
        }
    }

    private void OnExitDiashowMessageReceived(ExitDiashowMessage msg)
    {
        ShowUI = true;
        Messenger.Send(new ExitFullscreenMessage());
        displayRequest.DisposeSafely(() => displayRequest = null);
    }

    private void FlipViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FlipViewModel.SelectedItemModel))
        {
            var selectedItemModel = FlipViewModel.SelectedItemModel;

            if (detailsBarModel != null)
            {
                detailsBarModel.SelectedItemModel = selectedItemModel;
            }

            if (commandBarModel != null)
            {
                commandBarModel.SelectedItemModel = selectedItemModel;
            }

            if (metadataPanelModel != null)
            {
                metadataPanelModel.Files = selectedItemModel is not null ? [selectedItemModel.MediaFile] : [];
            }

            if (editImageOverlayModel != null)
            {
                editImageOverlayModel.File = selectedItemModel?.MediaFile as IBitmapFileInfo;
                editImageOverlayModel.Image = (selectedItemModel as IBitmapFlipViewItemModel)?.ImageViewModel.Image;
            }
        }
    }

    private IFlipViewPageCommandBarModel CreateCommandBarModel()
    {
        commandBarModel = viewModelFactory.CreateFlipViewPageCommandBarModel(FlipViewModel.SelectPreviousCommand, FlipViewModel.SelectNextCommand);
        commandBarModel.SelectedItemModel = FlipViewModel.SelectedItemModel;
        return commandBarModel;
    }

    private IMetadataPanelModel CreateMetadataPanelModel()
    {
        metadataPanelModel = viewModelFactory.CreateMetadataPanelModel(true);
        metadataPanelModel.Files = FlipViewModel.SelectedItemModel is not null ? [FlipViewModel.SelectedItemModel.MediaFile] : [];
        return metadataPanelModel;
    }

    private IDetailsBarModel CreateDetailsBarModel()
    {
        detailsBarModel = viewModelFactory.CreateDetailsBarModel();
        DetailsBarModel.SelectedItemModel = FlipViewModel.SelectedItemModel;
        return detailsBarModel;
    }

    private EditImageOverlayModel CreateEditImageOverlayModel()
    {
        editImageOverlayModel = viewModelFactory.CreateEditImageOverlayModel();
        EditImageOverlayModel.File = FlipViewModel.SelectedItemModel?.MediaFile as IBitmapFileInfo;
        EditImageOverlayModel.Image = (FlipViewModel.SelectedItemModel as IBitmapFlipViewItemModel)?.ImageViewModel.Image;
        return editImageOverlayModel;
    }
}
