using Essentials.NET;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels;

public partial class InfoBarModel : ViewModelBase
{
    private static readonly TimeSpan DefaultMessageDuration = TimeSpan.FromSeconds(3);

    public string Message { get; private set; } = string.Empty;

    public bool IsOpen => !string.IsNullOrEmpty(Message);

    private ITimer? timer;

    public void ShowMessage(string message)
    {
        Message = message;

        if (timer is null)
        {
            timer = TimeProvider.System.CreateTimer(DefaultMessageDuration, () =>
            {
                DispatchAsync(() => { Message = string.Empty; });
            });
        }
        else
        {
            timer.Restart(DefaultMessageDuration);
        }
    }
}

