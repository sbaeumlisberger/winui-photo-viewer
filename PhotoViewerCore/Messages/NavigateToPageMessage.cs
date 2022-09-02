using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerApp.Messages;

public record class NavigateToPageMessage(Type PageType, object? Parameter = null);
