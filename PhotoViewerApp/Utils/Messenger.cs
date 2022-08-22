using System;
using System.Collections.Generic;

namespace PhotoViewerApp.Utils;

public interface IMessenger
{
    void Publish<T>(T message) where T : notnull;

    void Subscribe<T>(Action<T> callback) where T : notnull;
}

internal class Messenger : IMessenger
{
    [ThreadStatic]
    private static IMessenger? instance;

    private Dictionary<Type, List<Delegate>> callbacksByMessageType = new Dictionary<Type, List<Delegate>>();

    public static IMessenger GetForCurrentThread()
    {
        if (instance is null)
        {
            instance = new Messenger();
        }
        return instance;
    }

    public void Publish<T>(T message) where T : notnull
    {
        if (callbacksByMessageType.TryGetValue(typeof(T), out var callbacksList))
        {
            callbacksList.ForEach(callback => ((Action<T>)callback).Invoke(message));
        }
    }

    public void Subscribe<T>(Action<T> callback) where T : notnull
    {
        if (callbacksByMessageType.TryGetValue(typeof(T), out var callbacksList))
        {
            callbacksList.Add(callback);
        }
        else
        {
            callbacksByMessageType.Add(typeof(T), new List<Delegate>() { callback });
        }
    }
}
