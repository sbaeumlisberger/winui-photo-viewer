using PhotoViewerCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.Utils;

public static class ApplicationSettingsProvider
{
    private static ApplicationSettings? settings;

    public static ApplicationSettings GetSettings() 
    {
        return settings ?? throw new InvalidOperationException("Settings not set");
    }

    public static void SetSettings(ApplicationSettings settings) 
    {
        ApplicationSettingsProvider.settings = settings;
    }

}
