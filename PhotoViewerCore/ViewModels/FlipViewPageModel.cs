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

        FlipViewModel = flipViewModelFactory.Invoke();
        DetailsBarModel = detailsBarModelFactory.Invoke();
        CommandBarModel = flipViewPageCommandBarModelFactory.Invoke(FlipViewModel);
        MetadataPanelModel = metadataPanelModelFactory.Invoke(true);

        FlipViewModel.PropertyChanged += FlipViewModel_PropertyChanged;

        messenger.Subscribe<StartDiashowMessage>(msg =>
        {
            DetailsBarModel.IsVisible = false;
            CommandBarModel.IsVisible = false;
            messenger.Publish(new EnterFullscreenMessage());
            try
            {
                displayRequest = displayRequestService.RequestActive();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to request display active", ex);
            }
        });

        messenger.Subscribe<ExitDiashowMessage>(msg =>
        {
            DetailsBarModel.IsVisible = true;
            CommandBarModel.IsVisible = true;
            messenger.Publish(new ExitFullscreenMessage());
            DisposeUtil.DisposeSafely(ref displayRequest);
        });
    }

    private void FlipViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FlipViewModel.SelectedItemModel))
        {
            DetailsBarModel.SelectedItemModel = FlipViewModel.SelectedItemModel;
            CommandBarModel.SelectedItemModel = FlipViewModel.SelectedItemModel;
        }
    }

    public void OnNavigatedTo(object navigationParameter)
    {
        FlipViewModel.SetItems(session.MediaItems, (IMediaFileInfo?)navigationParameter);
    }
}
