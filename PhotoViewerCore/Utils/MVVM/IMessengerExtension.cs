using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.Utils;

public static class IMessengerExtension
{
    public static void Register<TMessage>(this IMessenger messenger, object recipient, Action<TMessage> handler) where TMessage : class
    {
        messenger.Register<TMessage>(recipient, (_, msg) => handler(msg));
    }
}
