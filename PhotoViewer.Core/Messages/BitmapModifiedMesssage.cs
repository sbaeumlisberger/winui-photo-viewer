using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Messages;

public record class BitmapModifiedMesssage(IBitmapFileInfo BitmapFile);
