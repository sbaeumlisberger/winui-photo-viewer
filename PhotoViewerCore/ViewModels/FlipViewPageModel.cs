using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Services;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using System.ComponentModel;

namespace PhotoViewerApp.ViewModels;

public partial class FlipViewPageModel : ViewModelBase
{
    public IDetailsBarModel DetailsBarModel { get; }

    public IFlipViewPageCommandBarModel CommandBarModel { get; }

    public IMediaFlipViewModel FlipViewModel { get; }

    public IMetadataPanelModel MetadataPanelModel { get; }

    public bool ShowUI { get; private set; } = true;

    private readonly Session session;

    private readonly IDisplayRequestService displayRequestService;

    private IDisposable? displayRequest;

    public FlipViewPageModel(
        Session session,
        IMessenger messenger,
        Func<IMediaFlipViewModel> flipViewModelFactory,
        Func<IDetailsBarModel> detailsBarModelFactory,
        Func<IMediaFlipViewModel, IFlipViewPageCommandBarModel> flipViewPageCommandBarModelFactory,
        IDisplayRequestService displayRequestService,
        MetadataPanelModelFactory metadataPanelModelFactory) : base(messenger)
    {
        this.session = session;
        this.displayRequestService = displayRequestService;

        FlipViewModel = flipViewModelFactory.Invoke();
        DetailsBarModel = detailsBarModelFactory.Invoke();
        CommandBarModel = flipViewPageCommandBarModelFactory.Invoke(FlipViewModel);
        MetadataPanelModel = metadataPanelModelFactory.Invoke(true);

        FlipViewModel.PropertyChanged += FlipViewModel_PropertyChanged;

        Messenger.Register<StartDiashowMessage>(this, OnStartDiashowMessageReceived);
        Messenger.Register<ExitDiashowMessage>(this, OnExitDiashowMessageReceived);
    }

    public void OnNavigatedTo(object navigationParameter)
    {
        if (session.Files.Any())
        {
            FlipViewModel.SetItems(session.Files, (IMediaFileInfo?)navigationParameter);
        }
    }

    protected override void OnViewDisconnectedOverride()
    {
        FlipViewModel.PropertyChanged -= FlipViewModel_PropertyChanged;
        DisposeUtil.DisposeSafely(ref displayRequest);
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
        DisposeUtil.DisposeSafely(ref displayRequest);
    }

    private void FlipViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FlipViewModel.SelectedItemModel))
        {
            var selectedItemModel = FlipViewModel.SelectedItemModel;
            DetailsBarModel.SelectedItemModel = selectedItemModel;
            CommandBarModel.SelectedItemModel = selectedItemModel;
            MetadataPanelModel.Files = CollectionsUtil.ListOf(selectedItemModel?.MediaItem);
        }
    }

}
