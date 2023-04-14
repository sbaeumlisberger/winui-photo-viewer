using PhotoViewer.App.Models;
using PhotoViewer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Messages;

public record class BitmapImageLoadedMessage(IBitmapFileInfo BitmapFile, IBitmapImageModel BitmapImage);
