namespace PhotoViewerApp.Messages;

public record class NavigateToPageMessage(Type PageType, object? Parameter = null);
