using Essentials.NET;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels.Shared;
using System.Windows.Input;

namespace PhotoViewer.Core.ViewModels;

public partial class InfoBarModel : ViewModelBase
{
    public class InforBarMessage
    {
        public required string Text { get; init; }

        public InfoBarSeverity Severity { get; init; } = InfoBarSeverity.Informational;

        public ICommand? Command { get; init; }

        public string? CommandLabel { get; init; }

        public KeyboardAcceleratorViewModel? CommandKeyboardAccelerator { get; init; }

        public bool ShowActionButton => Command != null;
    }

    private static readonly TimeSpan DefaultMessageDuration = TimeSpan.FromSeconds(3);

    public partial InforBarMessage? Message { get; private set; }

    public bool IsOpen => Message is not null;

    private ITimer? timer;

    public void ShowMessage(InforBarMessage message)
    {
        Message = message;

        if (timer is null)
        {
            timer = TimeProvider.System.CreateTimer(DefaultMessageDuration, () =>
            {
                DispatchAsync(() => { Message = null; });
            });
        }
        else
        {
            timer.Restart(DefaultMessageDuration);
        }
    }

    public void ShowMessage(string text, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        ShowMessage(new InforBarMessage() { Text = text, Severity = severity });
    }

    public void HideMessage()
    {
        timer?.Stop();
        Message = null;
    }
}

