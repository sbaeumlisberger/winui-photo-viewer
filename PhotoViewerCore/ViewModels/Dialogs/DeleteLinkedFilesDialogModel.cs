using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.ViewModels;

public class DeleteLinkedFilesDialogModel
{  
    public bool IsYes { get; set; } = false;
    public bool IsRemember { get; set; } = false;
}
