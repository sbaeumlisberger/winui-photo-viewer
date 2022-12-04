namespace PhotoViewerApp.Utils;

public interface IMessenger
{
    void Publish<T>(T message) where T : notnull;

    void Subscribe<T>(Action<T> callback) where T : notnull;

    void Unsubscribe<T>(Action<T> callback) where T : notnull;
}

public class Messenger : IMessenger
{
    public static Messenger GlobalInstance { get; } = new Messenger();

    private Dictionary<Type, HashSet<Delegate>> callbacksByMessageType = new Dictionary<Type, HashSet<Delegate>>();

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
            callbacksByMessageType.Add(typeof(T), new HashSet<Delegate>() { callback });
        }
    }

    public void Unsubscribe<T>(Action<T> callback) where T : notnull
    {
        if (callbacksByMessageType.TryGetValue(typeof(T), out var callbacksList))
        {
            callbacksList.Remove(callback);
        }
    }
}
