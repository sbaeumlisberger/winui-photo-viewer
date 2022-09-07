namespace PhotoViewerApp.Utils;

public class DisposeUtil
{

    public static void DisposeSafely<T>(ref T? field) where T : IDisposable
    {
        var tmp = field;
        field = default;
        tmp?.Dispose();
    }
}
