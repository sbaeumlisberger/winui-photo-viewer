using PhotoViewerApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.ViewModels;

public interface IMediaFlipViewItemModel : INotifyPropertyChanged
{
    IMediaFileInfo MediaItem { get; }

    bool IsActive { get; set; }

    void StartLoading();

    void Cleanup();
}
