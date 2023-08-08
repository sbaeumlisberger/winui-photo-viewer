namespace PhotoViewer.Core.Utils;

public class DisposableAction : IDisposable
{
    private readonly Action action;

    public DisposableAction(Action action)
    {
        this.action = action;
    }

    public void Dispose()
    {
        action();
    }
}

