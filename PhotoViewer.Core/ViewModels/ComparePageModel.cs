using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Messages;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels;

public partial class ComparePageModel : ViewModelBase
{
    public CompareViewModel Left { get; }
    public CompareViewModel Right { get; }

    public bool IsLinkView { get; private set; } = true;

    private readonly IObservableList<IBitmapFileInfo> bitmapFiles = new ObservableList<IBitmapFileInfo>();

    public ComparePageModel(ApplicationSession session, IMessenger messenger, IImageLoaderService imageLoaderService, IDeleteFilesCommand deleteFilesCommand) : base(messenger)
    {
        bitmapFiles = new ObservableList<IBitmapFileInfo>(session.Files.OfType<IBitmapFileInfo>());

        Left = new CompareViewModel(bitmapFiles, imageLoaderService, deleteFilesCommand);
        Right = new CompareViewModel(bitmapFiles, imageLoaderService, deleteFilesCommand);

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
        Left.SelectedBitmapFile = (IBitmapFileInfo?)navigationParameter ?? bitmapFiles.First();
        Right.SelectedBitmapFile = bitmapFiles.GetSuccessor(Left.SelectedBitmapFile);
    }

    private void Left_ViewChanged(object? sender, CompareViewModel.ViewState e)
    {
        if (IsLinkView)
        {
            Right.ChangeView(e.ZoomFactor, e.HorizontalOffset, e.VerticalOffset);
        }
    }

    private void Right_ViewChanged(object? sender, CompareViewModel.ViewState e)
    {
        if (IsLinkView)
        {
            Left.ChangeView(e.ZoomFactor, e.HorizontalOffset, e.VerticalOffset);
        }
    }

    private void OnReceive(MediaFilesDeletedMessage msg)
    {
        msg.Files.OfType<IBitmapFileInfo>().ForEach(mediaItem => bitmapFiles.Remove(mediaItem));
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
