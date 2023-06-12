using PhotoViewer.App.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels;

public partial class DeleteLinkedFilesDialogModel : ViewModelBase
{  
    public bool IsYes { get; set; } = false;
    public bool IsRemember { get; set; } = false;
}
