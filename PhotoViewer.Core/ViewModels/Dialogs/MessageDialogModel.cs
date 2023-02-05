using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels
{
    public class MessageDialogModel
    {
        public required string Title { get; set; }
        public required string Message { get; set; }
    }
}
