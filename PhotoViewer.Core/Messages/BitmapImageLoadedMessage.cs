using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

public record class BitmapImageLoadedMessage(IBitmapFileInfo BitmapFile, IBitmapImageModel BitmapImage);
