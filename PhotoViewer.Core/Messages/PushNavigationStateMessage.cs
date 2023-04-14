using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Messages;

internal record class PushNavigationStateMessage(object NavigationState);
