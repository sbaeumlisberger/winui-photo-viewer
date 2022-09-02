using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerApp.Services;

public interface IDialogService
{
    Task ShowDialogAsync(object dialogModel);
}
