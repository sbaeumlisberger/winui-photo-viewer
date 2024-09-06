using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace PhotoViewer.Core.Utils;

public static class IMessengerExtension
{
    public static void Register<TMessage>(this IMessenger messenger, object recipient, Action<TMessage> handler) where TMessage : class
    {
        messenger.Register<TMessage>(recipient, (_, msg) => handler(msg));
    }

    public static TResponse Request<TRequestMessage, TResponse>(this IMessenger messenger, TRequestMessage message, TResponse defaultValue) where TRequestMessage : RequestMessage<TResponse>
    {
        messenger.Send(message);
        return message.HasReceivedResponse ? message.Response : defaultValue;
    }
}
