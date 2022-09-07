using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.Models;

public class ApplicationSettings
{
    public bool ShowDeleteAnimation { get; set; } = true;
    public bool AutoOpenMetadataPanel { get; set; } = false;
    public bool AutoOpenDetailsBar { get; set; } = false; 
    public TimeSpan DiashowTime { get; set; } = TimeSpan.FromSeconds(3);

    public bool LinkRawFiles { get; set; } = true;
    public string RawFilesFolderName { get; set; } = "RAWs";
    public DeleteLinkedFilesOption DeleteLinkedFilesOption { get; set; } = DeleteLinkedFilesOption.Ask;

    public bool IncludeVideos { get; set; } = true;
}