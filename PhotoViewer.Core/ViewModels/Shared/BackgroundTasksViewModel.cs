using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Specialized;

namespace PhotoViewer.Core.ViewModels.Shared;

public partial class BackgroundTasksViewModel : ViewModelBase
{
    public bool ShowProgressIndicator { get; private set; } = false;

    public string StatusText { get; private set; } = "";

    private readonly IBackgroundTaskService backgroundTaskService;

    public BackgroundTasksViewModel(IBackgroundTaskService backgroundTaskService)
    {
        this.backgroundTaskService = backgroundTaskService;
        backgroundTaskService.BackgroundTasks.CollectionChanged += BackgroundTasks_CollectionChanged;
    }

    private void BackgroundTasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatchAsync(() =>
        {
            var backgroundTasks = backgroundTaskService.BackgroundTasks;
            ShowProgressIndicator = backgroundTasks.Count > 0;
            StatusText = backgroundTasks.Count == 0 ? "" : backgroundTasks.Count == 1
                ? Strings.BackgroundTasksRunningMessage_Single
                : string.Format(Strings.BackgroundTasksRunningMessage_Multiple, backgroundTasks.Count);
        });
    }
}
