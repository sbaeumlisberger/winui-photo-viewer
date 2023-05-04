using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoViewer.Core;

public static class AppData
{
    public const string MapServiceToken = "vQDj7umE60UMzHG2XfCm~ehfqvBJAFQn6pphOPVbDsQ~ArtM_t2j4AyKdgLIa5iXeftg8bEG4YRYCwhUN-SMXhIK73mnPtCYU4nOF2VtqGiF";

    public static readonly string LocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "PhotoViewer");

    static AppData()
    {
        Directory.CreateDirectory(LocalFolder);
    }
}
