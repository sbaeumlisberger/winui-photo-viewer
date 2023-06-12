using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Services;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels;

public class MainWindowModel
{
    private DropOutStack<object> navigationStateStack = new DropOutStack<object>(20);

    private readonly IBackgroundTaskService backgroundTaskService;

    private readonly IDialogService dialogService;

    public MainWindowModel(IMessenger messenger, IBackgroundTaskService backgroundTaskService, IDialogService dialogService)
    {
        this.backgroundTaskService = backgroundTaskService;
        this.dialogService = dialogService;
        messenger.Register<PopNavigationStateMessage>(this, Received);
        messenger.Register<PushNavigationStateMessage>(this, Received);
    }

    public async Task OnClosingAsync()
    {
        var dialogTask = dialogService.ShowDialogAsync(new MessageDialogModel()
        {
            Title = Strings.WaitForBackgroundTaskDialog_Title,
            Message = Strings.WaitForBackgroundTaskDialog_Message,
            PrimaryButtonText = Strings.WaitForBackgroundTaskDialog_PrimaryButton,
        });

        await Task.WhenAny(dialogTask, Task.WhenAll(backgroundTaskService.BackgroundTasks));
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
}
