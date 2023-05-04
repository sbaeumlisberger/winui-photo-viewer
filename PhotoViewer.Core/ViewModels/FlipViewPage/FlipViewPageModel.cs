using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Models;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.ComponentModel;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core;

namespace PhotoViewer.App.ViewModels;

public partial class FlipViewPageModel : ViewModelBase
{
    public IDetailsBarModel DetailsBarModel { get; }

    public IFlipViewPageCommandBarModel CommandBarModel { get; }

    public IMediaFlipViewModel FlipViewModel { get; }

    public IMetadataPanelModel MetadataPanelModel { get; }

    public bool ShowUI { get; private set; } = true;

    private readonly ApplicationSession session;

    private readonly IDisplayRequestService displayRequestService;

    private IDisposable? displayRequest;

    public FlipViewPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IViewModelFactory viewModelFactory,
        IDisplayRequestService displayRequestService) : base(messenger)
    {
        this.session = session;
        this.displayRequestService = displayRequestService;

        FlipViewModel = viewModelFactory.CreateMediaFlipViewModel();
        DetailsBarModel = viewModelFactory.CreateDetailsBarModel();
        CommandBarModel = viewModelFactory.CreateFlipViewPageCommandBarModel(FlipViewModel.SelectPreviousCommand, FlipViewModel.SelectNextCommand);
        MetadataPanelModel = viewModelFactory.CreateMetadataPanelModel(true);

        FlipViewModel.PropertyChanged += FlipViewModel_PropertyChanged;

        Messenger.Register<StartDiashowMessage>(this, OnStartDiashowMessageReceived);
        Messenger.Register<ExitDiashowMessage>(this, OnExitDiashowMessageReceived);
    }

    protected override void OnCleanup()
    {
        FlipViewModel.PropertyChanged -= FlipViewModel_PropertyChanged;
        DisposeUtil.DisposeSafely(ref displayRequest);

        FlipViewModel.Cleanup();
        DetailsBarModel.Cleanup();
        CommandBarModel.Cleanup();
        MetadataPanelModel.Cleanup();
    }

    public void OnNavigatedTo(object navigationParameter, bool popNavigationState)
    {
        IMediaFileInfo? restoredMediaFile = null;

        if (popNavigationState && Messenger.Send<PopNavigationStateMessage>().Response is Dictionary<string, object> navigationState)
        {
            restoredMediaFile = (IMediaFileInfo?)navigationState[nameof(FlipViewModel.SelectedItem)];

            if (!session.Files.Contains(restoredMediaFile)) 
            {
                restoredMediaFile = null;
            }
        }

        if (session.Files.Any())
        {
            FlipViewModel.SetItems(session.Files, (IMediaFileInfo?)navigationParameter ?? restoredMediaFile);
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
