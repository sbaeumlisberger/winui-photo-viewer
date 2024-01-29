namespace PhotoViewer.Core.ViewModels
{
    public class MessageDialogModel
    {
        public required string Title { get; set; }
        public required string Message { get; set; }
        public string? PrimaryButtonText { get; set; }
    }
}
