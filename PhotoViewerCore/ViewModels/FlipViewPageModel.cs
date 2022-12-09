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
    }

    public void OnNavigatedTo(object navigationParameter)
    {
        FlipViewModel.PropertyChanged += FlipViewModel_PropertyChanged;
        FlipViewModel.SetItems(session.MediaItems, (IMediaFileInfo?)navigationParameter);
    }

    protected override void OnViewConnectedOverride()
    {
        Messenger.Register<StartDiashowMessage>(this, OnStartDiashowMessageReceived);
        Messenger.Register<ExitDiashowMessage>(this, OnExitDiashowMessageReceived);
    }

    protected override void OnViewDisconnectedOverride()
    {
        FlipViewModel.PropertyChanged -= FlipViewModel_PropertyChanged;
    }

    private void OnStartDiashowMessageReceived(StartDiashowMessage msg)
    {
        DetailsBarModel.IsVisible = false;
        CommandBarModel.IsVisible = false;
        MetadataPanelModel.IsVisible = false;
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
        DetailsBarModel.IsVisible = true;
        CommandBarModel.IsVisible = true;
        MetadataPanelModel.IsVisible = true;
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
