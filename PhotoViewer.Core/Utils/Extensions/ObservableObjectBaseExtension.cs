using static PhotoViewer.Core.Utils.ObservableObjectBase;

namespace PhotoViewer.Core.Utils;

public static class ObservableObjectBaseExtension
{
    public static Subsciption Subscribe<T>(this T observableObject, object subscriber, string propertyName, Action<T> callback, bool initialCallback = false) where T : ObservableObjectBase
    {
        return observableObject.Subscribe(subscriber, propertyName, () => callback(observableObject), initialCallback);
    }
}
