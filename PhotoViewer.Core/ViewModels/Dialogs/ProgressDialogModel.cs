using PhotoViewer.App.Utils;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels;

public partial class ProgressDialogModel : ViewModelBase
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required Progress Progress { get; init; }
}
