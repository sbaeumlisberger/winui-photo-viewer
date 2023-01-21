using PhotoViewerApp.Utils.Logging;
using Windows.System.Display;

namespace PhotoViewerApp.Services;

public interface IDisplayRequestService
{
    IDisposable RequestActive();
}

internal class DisplayRequestService : IDisplayRequestService
{
    public class DisposableDisplayRequest : IDisposable
    {
        private DisplayRequest? displayRequest;

        public DisposableDisplayRequest(DisplayRequest displayRequest)
        {
            this.displayRequest = displayRequest;
        }

        public void Dispose()
        {
            try
            {
                displayRequest?.RequestRelease();
                displayRequest = null!;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to release display request", ex);
            }
        }
    }

    public IDisposable RequestActive()
    {
        var displayRequest = new DisplayRequest();
        displayRequest.RequestActive(); // TODO throws always a COMException 0x80040200 (https://github.com/microsoft/CsWinRT/issues/962)
        return new DisposableDisplayRequest(displayRequest);
    }
}
