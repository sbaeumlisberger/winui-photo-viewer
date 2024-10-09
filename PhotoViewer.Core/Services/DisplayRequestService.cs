using Essentials.NET.Logging;
using Windows.System.Display;

namespace PhotoViewer.Core.Services;

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
        displayRequest.RequestActive();
        return new DisposableDisplayRequest(displayRequest);
    }
}
