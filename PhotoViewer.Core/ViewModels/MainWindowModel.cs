using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.Core.Messages;
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

    public MainWindowModel(IMessenger messenger)
    {
        messenger.Register<PopNavigationStateMessage>(this, Received);
        messenger.Register<PushNavigationStateMessage>(this, Received);
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
