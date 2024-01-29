using CommunityToolkit.Mvvm.Messaging;

namespace PhotoViewer.Core.Utils;

public static class IMessengerExtension
{
    public static void Register<TMessage>(this IMessenger messenger, object recipient, Action<TMessage> handler) where TMessage : class
    {
        messenger.Register<TMessage>(recipient, (_, msg) => handler(msg));
    }
}
