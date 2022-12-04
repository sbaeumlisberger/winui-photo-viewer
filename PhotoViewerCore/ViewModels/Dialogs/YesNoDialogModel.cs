using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.ViewModels;

public class YesNoDialogModel
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsYes { get; set; } = false;
    public string RememberMessage { get; set; } = string.Empty;
    public bool IsRemember { get; set; } = false;
}
