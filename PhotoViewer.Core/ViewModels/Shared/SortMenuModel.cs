using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels.Shared;

public partial class SortMenuModel : ViewModelBase
{
    public bool IsSortedByFileName { get; set; }

    public bool IsSortedByFileSize { get; set; }

    public bool IsSortedByDateTaken { get; set; }

    public bool IsSortedAscending { get; set; }

    public bool IsSortedDescending { get; set; }

    private readonly SortService sortService;

    private readonly ApplicationSession applicationSession;

    internal SortMenuModel(ApplicationSession applicationSession, SortService sortService, IMessenger messenger) : base(messenger)
    {
        this.applicationSession = applicationSession;
        this.sortService = sortService;

        IsSortedByFileName = applicationSession.SortBy == SortBy.FileName;
        IsSortedByFileSize = applicationSession.SortBy == SortBy.FileSize;
        IsSortedByDateTaken = applicationSession.SortBy == SortBy.DateTaken;
        IsSortedAscending = !applicationSession.IsSortedDescending;
        IsSortedDescending = applicationSession.IsSortedDescending;
    }


    [RelayCommand]
    private async Task SortByFileNameAsync()
    {
        await SortAsync(SortBy.FileName, applicationSession.IsSortedDescending);
    }

    [RelayCommand]
    private async Task SortByFileSizeAsync()
    {
        await SortAsync(SortBy.FileSize, applicationSession.IsSortedDescending);
    }

    [RelayCommand]
    private async Task SortByDateTakenAsync()
    {
        await SortAsync(SortBy.DateTaken, applicationSession.IsSortedDescending);
    }

    [RelayCommand]
    private async Task ToggleSortOrderAsync()
    {
        await SortAsync(applicationSession.SortBy, !applicationSession.IsSortedDescending);
    }

    private async Task SortAsync(SortBy sortBy, bool descending)
    {
        var sortedFiles = await sortService.SortAsync(applicationSession.Files, sortBy, descending);
        Messenger.Send(new FilesSortedMessage(sortedFiles, sortBy, descending));
        IsSortedByFileName = sortBy == SortBy.FileName;
        IsSortedByFileSize = sortBy == SortBy.FileSize;
        IsSortedByDateTaken = sortBy == SortBy.DateTaken;
        IsSortedAscending = !descending;
        IsSortedDescending = descending;
    }

}
