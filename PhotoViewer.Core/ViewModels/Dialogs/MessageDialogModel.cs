using PhotoViewer.Core.Resources;

namespace PhotoViewer.Core.ViewModels
{
    public class MessageDialogModel
    {
        public required string Title { get; set; }
        public required string Message { get; set; }
        public string? CloseButtonText { get; set; } = Strings.MessageDialog_CloseButtonText;
        public string? PrimaryButtonText { get; set; }
        public string? SecondaryButtonText { get; set; }
        public bool WasPrimaryButtonActivated { get; set; }
        public bool WasSecondaryButtonActivated { get; set; }
    }
}
