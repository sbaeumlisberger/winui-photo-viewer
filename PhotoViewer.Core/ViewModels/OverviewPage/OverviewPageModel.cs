using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.ViewModels;
using System.Collections.ObjectModel;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.Models;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core;
using PhotoViewer.App.Utils.Logging;

namespace PhotoViewer.App.ViewModels;

public partial class OverviewPageModel : ViewModelBase
{
    public IMetadataPanelModel MetadataPanelModel { get; }

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    public IObservableReadOnlyList<IMediaFileInfo> Items { get; private set; } = new ObservableList<IMediaFileInfo>();

    public IReadOnlyList<IMediaFileInfo> SelectedItems { get; set; } = Array.Empty<IMediaFileInfo>();

    public IOverviewPageCommandBarModel OverviewPageCommandBarModel { get; }

    public bool ShowLoadingUI { get; set; } = false;

    private readonly IViewModelFactory viewModelFactory;

    private readonly Dictionary<IMediaFileInfo, IOverviewItemModel> itemModels = new Dictionary<IMediaFileInfo, IOverviewItemModel>();

    public OverviewPageModel(
        ApplicationSession session,
        IMessenger messenger,
        IViewModelFactory viewModelFactory) : base(messenger)
    {
        this.viewModelFactory = viewModelFactory;

        OverviewPageCommandBarModel = viewModelFactory.CreateOverviewPageCommandBarModel();
        MetadataPanelModel = viewModelFactory.CreateMetadataPanelModel(false);
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel(isRenameFilesEnabled: true);

        Messenger.Register<MediaFilesLoadingMessage>(this, OnReceived);
        Messenger.Register<MediaFilesDeletedMessage>(this, OnReceived);

        Items = new ObservableList<IMediaFileInfo>(session.Files);
    }

    protected override void OnCleanup()
    {
        OverviewPageCommandBarModel.Cleanup();
        MetadataPanelModel.Cleanup();
        ContextMenuModel.Cleanup();
        itemModels.Values.ForEach(itemModel => itemModel.Cleanup());
        itemModels.Clear();
    }

    public void ShowItem(IMediaFileInfo mediaItem)
    {
        Messenger.Send(new NavigateToPageMessage(typeof(FlipViewPageModel), mediaItem));
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
            // TODO show dialog
            Log.Error("Failed to load files", ex);
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
