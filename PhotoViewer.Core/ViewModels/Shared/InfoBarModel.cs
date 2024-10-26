using Essentials.NET;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using System.Windows.Input;

namespace PhotoViewer.Core.ViewModels;

public partial class InfoBarModel : ViewModelBase
{
    private static readonly TimeSpan DefaultMessageDuration = TimeSpan.FromSeconds(3);

    public string Message { get; private set; } = string.Empty;

    public bool IsOpen => !string.IsNullOrEmpty(Message);

    public InfoBarSeverity Severity { get; private set; } = InfoBarSeverity.Informational;

    public ICommand? Command { get; private set; }

    public string? CommandLabel { get; private set; }

    public bool ShowActionButton => Command != null;

    private ITimer? timer;

    public void ShowMessage(string message, InfoBarSeverity severity = InfoBarSeverity.Informational, ICommand? command = null, string? commandLabel = null)
    {
        Message = message;
        Severity = severity;
        Command = command;
        CommandLabel = commandLabel;

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

    public void HideMessage()
    {
        Message = string.Empty;
        timer?.Stop();
    }
}

