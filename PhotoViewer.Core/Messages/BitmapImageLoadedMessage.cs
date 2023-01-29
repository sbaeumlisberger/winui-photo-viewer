using PhotoViewer.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.Messages;

public record class BitmapImageLoadedMessage(IBitmapFileInfo BitmapFile, IBitmapImage BitmapImage);
