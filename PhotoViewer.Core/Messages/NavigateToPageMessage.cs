namespace PhotoViewer.App.Messages;

public record class NavigateToPageMessage(Type PageType, object? Parameter = null);
