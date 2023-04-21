namespace PhotoViewer.App.Utils;

public static class DisposeUtil
{

    public static void DisposeSafely<T>(ref T? field) where T : IDisposable
    {
        var tmp = field;
        field = default;
        tmp?.Dispose();
    }

    public static void DisposeSafely(this IDisposable? disposable, Action beforeDispose)
    {
        beforeDispose();
        disposable?.Dispose();
    }
}
