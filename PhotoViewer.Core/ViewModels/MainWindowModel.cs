using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class MainWindowModel : ViewModelBase
{
    public event EventHandler<DialogRequestedEventArgs>? DialogRequested;

    public string Title { get; private set; } = AppData.ApplicationName;

    public AppTheme Theme { get; private set; }

    private readonly DropOutStack<object> navigationStateStack = new DropOutStack<object>(20);

    private readonly ApplicationSettings settings;

    private readonly IBackgroundTaskService backgroundTaskService;

    private readonly IDialogService dialogService;

    internal MainWindowModel(
        ApplicationSettings settings,
        IMessenger messenger,
        IBackgroundTaskService backgroundTaskService,
        IDialogService dialogService)
        : base(messenger)
    {
        this.settings = settings;
        this.backgroundTaskService = backgroundTaskService;
        this.dialogService = dialogService;
        dialogService.DialogRequested += DialogService_DialogRequested;
        messenger.Register<ChangeWindowTitleMessage>(this, Received);
        messenger.Register<PopNavigationStateMessage>(this, Received);
        messenger.Register<PushNavigationStateMessage>(this, Received);
        messenger.Register<SettingsChangedMessage>(this, Received);
    }

    protected override void OnCleanup()
    {
        dialogService.DialogRequested -= DialogService_DialogRequested;
    }

    private void DialogService_DialogRequested(object? sender, DialogRequestedEventArgs e)
    {
        DialogRequested?.Invoke(this, e);
    }

    public async Task OnClosingAsync()
    {
        var backgroundTasks = backgroundTaskService.BackgroundTasks.ToList();

        if (backgroundTasks.Count != 0)
        {
            var dialogTask = dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.WaitForBackgroundTaskDialog_Title,
                Message = Strings.WaitForBackgroundTaskDialog_Message,
                PrimaryButtonText = Strings.WaitForBackgroundTaskDialog_PrimaryButton,
            });

            await Task.WhenAny(dialogTask, Task.WhenAll(backgroundTasks));
        }
    }
    private void Received(ChangeWindowTitleMessage msg)
    {
        Title = msg.NewTitle + " - " + AppData.ApplicationName;
    }

    private void Received(PopNavigationStateMessage msg)
    {
        navigationStateStack.TryPop(out object? navigationState);
        msg.Reply(navigationState);
    }

    private void Received(PushNavigationStateMessage msg)
    {
        navigationStateStack.Push(msg.NavigationState);
    }

    private void Received(SettingsChangedMessage msg)
    {
        Theme = settings.Theme;
    }
}
