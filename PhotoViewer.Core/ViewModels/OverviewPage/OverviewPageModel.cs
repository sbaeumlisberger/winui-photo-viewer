using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class OverviewPageModel : ViewModelBase
{
    public IMetadataPanelModel MetadataPanelModel { get; }

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    public IObservableReadOnlyList<IMediaFileInfo> Items { get; private set; } = new ObservableList<IMediaFileInfo>();

    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IOverviewPageCommandBarModel OverviewPageCommandBarModel { get; }

    public bool ShowLoadingUI { get; set; } = false;

    private readonly IViewModelFactory viewModelFactory;

    private readonly IDialogService dialogService;

    private readonly Dictionary<IMediaFileInfo, IOverviewItemModel> itemModels = new Dictionary<IMediaFileInfo, IOverviewItemModel>();

    internal OverviewPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IViewModelFactory viewModelFactory, IDialogService dialogService) : base(messenger)
    {
        this.viewModelFactory = viewModelFactory;
        this.dialogService = dialogService;

        OverviewPageCommandBarModel = viewModelFactory.CreateOverviewPageCommandBarModel();
        MetadataPanelModel = viewModelFactory.CreateMetadataPanelModel(false);
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel(isRenameFilesEnabled: true);

        Items = new ObservableList<IMediaFileInfo>(session.Files);

        Register<MediaFilesLoadingMessage>(OnReceived);
        Register<MediaFilesDeletedMessage>(OnReceived);
        Register<FilesSortedMessage>(msg => Items = new ObservableList<IMediaFileInfo>(session.Files));
        Register<SelectFilesMessage>(msg => SelectedItems = msg.FilesToSelect);
    }

    protected override void OnCleanup()
    {
        OverviewPageCommandBarModel.Cleanup();
        MetadataPanelModel.Cleanup();
        ContextMenuModel.Cleanup();
        itemModels.Values.ForEach(itemModel => itemModel.Cleanup());
        itemModels.Clear();
    }

    public void OnNavigatedTo()
    {
        Messenger.Send(new ChangeWindowTitleMessage(Strings.OverviewPage_Title));
    }

    public void ShowItem(IMediaFileInfo mediaFile)
    {
        Messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel), mediaFile));
    }

    public IOverviewItemModel GetItemModel(IMediaFileInfo mediaFile)
    {
        if (!itemModels.TryGetValue(mediaFile, out var itemModel))
        {
            itemModel = viewModelFactory.CreateOverviewItemModel(mediaFile);
            itemModels.Add(mediaFile, itemModel);
        }
        return itemModel;
    }

    private async void OnReceived(MediaFilesLoadingMessage msg)
    {
        try
        {
            ShowLoadingUI = true;

            SelectedItems = Array.Empty<IMediaFileInfo>();
            Items = new ObservableList<IMediaFileInfo>();
            itemModels.Values.ForEach(itemModel => itemModel.Cleanup());
            itemModels.Clear();

            Items = new ObservableList<IMediaFileInfo>((await msg.LoadMediaFilesTask.WaitForResultAsync()).MediaFiles);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load files", ex);

            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.LoadFilesErrorDialog_Title,
                Message = Strings.LoadFilesErrorDialog_Message,
            });
        }
        finally
        {
            ShowLoadingUI = false;
        }
    }

    private void OnReceived(MediaFilesDeletedMessage msg)
    {
        msg.Files.ForEach(mediaFile => ((ObservableList<IMediaFileInfo>)Items).Remove(mediaFile));
        msg.Files.ForEach(mediaFile => { itemModels[mediaFile].Cleanup(); itemModels.Remove(mediaFile); });
    }

    partial void OnSelectedItemsChanged()
    {
        OverviewPageCommandBarModel.SelectedItems = SelectedItems;
        MetadataPanelModel.Files = SelectedItems;
        ContextMenuModel.Files = SelectedItems;
    }

}
