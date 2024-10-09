using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class ComparePageModel : ViewModelBase
{
    public ICompareViewModel Left { get; }
    public ICompareViewModel Right { get; }

    public bool IsLinkView { get; private set; } = true;

    private readonly IObservableList<IBitmapFileInfo> bitmapFiles = new ObservableList<IBitmapFileInfo>();

    public ComparePageModel(IApplicationSession session, IMessenger messenger, IViewModelFactory viewModelFactory) : base(messenger)
    {
        bitmapFiles = new ObservableList<IBitmapFileInfo>(session.Files.OfType<IBitmapFileInfo>());

        Left = viewModelFactory.CreateCompareViewModel(bitmapFiles);
        Right = viewModelFactory.CreateCompareViewModel(bitmapFiles);

        Left.ViewChangedByUser += Left_ViewChanged;
        Right.ViewChangedByUser += Right_ViewChanged;

        Messenger.Register<MediaFilesDeletedMessage>(this, OnReceive);
    }

    protected override void OnCleanup()
    {
        Left.Cleanup();
        Right.Cleanup();
    }

    public void OnNavigatedTo(object navigationParameter)
    {
        Messenger.Send(new ChangeWindowTitleMessage(Strings.ComparePage_Title));
        Left.SelectedBitmapFile = (IBitmapFileInfo)navigationParameter;
        Right.SelectedBitmapFile = bitmapFiles.GetSuccessor(Left.SelectedBitmapFile) ?? Left.SelectedBitmapFile;
    }

    private void Left_ViewChanged(object? sender, ViewState e)
    {
        if (IsLinkView)
        {
            Right.ChangeView(e.ZoomFactor, e.HorizontalOffset, e.VerticalOffset);
        }
    }

    private void Right_ViewChanged(object? sender, ViewState e)
    {
        if (IsLinkView)
        {
            Left.ChangeView(e.ZoomFactor, e.HorizontalOffset, e.VerticalOffset);
        }
    }

    private void OnReceive(MediaFilesDeletedMessage msg)
    {
        msg.Files.OfType<IBitmapFileInfo>().ForEach(bitmapFile => bitmapFiles.Remove(bitmapFile));
    }


    [RelayCommand]
    private void NavigateBack()
    {
        Messenger.Send(new NavigateBackMessage());
    }

    [RelayCommand]
    private void ToggleLinkView()
    {
        IsLinkView = !IsLinkView;
    }

}
