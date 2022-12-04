using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.ViewModels;
using System.ComponentModel;

namespace PhotoViewerApp.ViewModels;

public partial class FlipViewPageModel : ViewModelBase
{
    public IDetailsBarModel DetailsBarModel { get; }

    public IFlipViewPageCommandBarModel CommandBarModel { get; }

    public IMediaFlipViewModel FlipViewModel { get; }

    public IMetadataPanelModel MetadataPanelModel { get; }

    private readonly Session session;

    private readonly IMessenger messenger;

    private readonly IDisplayRequestService displayRequestService;

    private IDisposable? displayRequest;

    public FlipViewPageModel(
        Session session,
        IMessenger messenger,
        Func<IMediaFlipViewModel> flipViewModelFactory,
        Func<IDetailsBarModel> detailsBarModelFactory,
        Func<IMediaFlipViewModel, IFlipViewPageCommandBarModel> flipViewPageCommandBarModelFactory,
        IDisplayRequestService displayRequestService,
        MetadataPanelModelFactory metadataPanelModelFactory)
    {
        this.session = session;
        this.messenger = messenger;
        this.displayRequestService = displayRequestService;

        FlipViewModel = flipViewModelFactory.Invoke();
        DetailsBarModel = detailsBarModelFactory.Invoke();
        CommandBarModel = flipViewPageCommandBarModelFactory.Invoke(FlipViewModel);
        MetadataPanelModel = metadataPanelModelFactory.Invoke(true);

        FlipViewModel.PropertyChanged += FlipViewModel_PropertyChanged;
    }

    public override void OnViewConnected()
    {
        messenger.Subscribe<StartDiashowMessage>(OnStartDiashowMessageReceived);
        messenger.Subscribe<ExitDiashowMessage>(OnExitDiashowMessageReceived);
    }

    public override void OnViewDisconnected()
    {
        messenger.Unsubscribe<StartDiashowMessage>(OnStartDiashowMessageReceived);
        messenger.Unsubscribe<ExitDiashowMessage>(OnExitDiashowMessageReceived);
    }

    private void OnStartDiashowMessageReceived(StartDiashowMessage msg)
    {
        DetailsBarModel.IsVisible = false;
        CommandBarModel.IsVisible = false;
        MetadataPanelModel.IsVisible = false;
        messenger.Publish(new EnterFullscreenMessage());
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
        DetailsBarModel.IsVisible = true;
        CommandBarModel.IsVisible = true;
        MetadataPanelModel.IsVisible = true;
        messenger.Publish(new ExitFullscreenMessage());
        DisposeUtil.DisposeSafely(ref displayRequest);
    }

    private void FlipViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FlipViewModel.SelectedItemModel))
        {
            var selectedItemModel = FlipViewModel.SelectedItemModel;
            DetailsBarModel.SelectedItemModel = selectedItemModel;
            CommandBarModel.SelectedItemModel = selectedItemModel;
            MetadataPanelModel.Files = CollectionsUtil.NotNull(selectedItemModel?.MediaItem);
        }
    }

    public void OnNavigatedTo(object navigationParameter)
    {
        FlipViewModel.SetItems(session.MediaItems, (IMediaFileInfo?)navigationParameter);
    }
}
