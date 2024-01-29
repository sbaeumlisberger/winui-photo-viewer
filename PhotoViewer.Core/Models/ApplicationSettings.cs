using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace PhotoViewer.Core.Models;

public partial class ApplicationSettings : ObservableObject
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public bool ShowDeleteAnimation { get; set; } = true;
    public bool AutoOpenMetadataPanel { get; set; } = false;
    public bool AutoOpenDetailsBar { get; set; } = false;
    public TimeSpan DiashowTime { get; set; } = TimeSpan.FromSeconds(3);

    public bool LinkRawFiles { get; set; } = true;
    public string RawFilesFolderName { get; set; } = "RAWs";
    public DeleteLinkedFilesOption DeleteLinkedFilesOption { get; set; } = DeleteLinkedFilesOption.Ask;

    public bool IncludeVideos { get; set; } = true;

    public bool IsDebugLogEnabled { get; set; } = true;

    public ApplicationSettings()
    {
#if DEBUG
        // enable debug log by default
        IsDebugLogEnabled = true;
#endif
    }

    public void Apply(ApplicationSettings settings)
    {
        Theme = settings.Theme;
        ShowDeleteAnimation = settings.ShowDeleteAnimation;
        AutoOpenMetadataPanel = settings.AutoOpenMetadataPanel;
        AutoOpenDetailsBar = settings.AutoOpenDetailsBar;
        DiashowTime = settings.DiashowTime;
        LinkRawFiles = settings.LinkRawFiles;
        RawFilesFolderName = settings.RawFilesFolderName;
        DeleteLinkedFilesOption = settings.DeleteLinkedFilesOption;
        IncludeVideos = settings.IncludeVideos;
        IsDebugLogEnabled = settings.IsDebugLogEnabled;
    }

    public static ApplicationSettings Deserialize(string serialized)
    {
        var keyValueMap = new Dictionary<string, string>();
        var reader = new StringReader(serialized);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            int equlasSignIndex = line.IndexOf('=');
            if (equlasSignIndex != -1)
            {
                string key = line.Substring(0, equlasSignIndex).Trim();
                string value = line.Substring(equlasSignIndex + 1).Trim();
                keyValueMap.Add(key, value);
            }
        }
        var settings = new ApplicationSettings();
        settings.Theme = (AppTheme)Enum.Parse(typeof(AppTheme), keyValueMap[nameof(Theme)]);
        settings.ShowDeleteAnimation = bool.Parse(keyValueMap[nameof(ShowDeleteAnimation)]);
        settings.AutoOpenMetadataPanel = bool.Parse(keyValueMap[nameof(AutoOpenMetadataPanel)]);
        settings.AutoOpenDetailsBar = bool.Parse(keyValueMap[nameof(AutoOpenDetailsBar)]);
        settings.DiashowTime = TimeSpan.Parse(keyValueMap[nameof(DiashowTime)]);
        settings.LinkRawFiles = bool.Parse(keyValueMap[nameof(LinkRawFiles)]);
        settings.RawFilesFolderName = keyValueMap[nameof(RawFilesFolderName)];
        settings.DeleteLinkedFilesOption = (DeleteLinkedFilesOption)Enum.Parse(typeof(DeleteLinkedFilesOption), keyValueMap[nameof(DeleteLinkedFilesOption)]);
        settings.IncludeVideos = bool.Parse(keyValueMap[nameof(IncludeVideos)]);
        settings.IsDebugLogEnabled = bool.Parse(keyValueMap[nameof(IsDebugLogEnabled)]);
        return settings;
    }

    public string Serialize()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(nameof(Theme) + "=" + Theme);
        stringBuilder.AppendLine(nameof(ShowDeleteAnimation) + "=" + ShowDeleteAnimation);
        stringBuilder.AppendLine(nameof(AutoOpenMetadataPanel) + "=" + AutoOpenMetadataPanel);
        stringBuilder.AppendLine(nameof(AutoOpenDetailsBar) + "=" + AutoOpenDetailsBar);
        stringBuilder.AppendLine(nameof(DiashowTime) + "=" + DiashowTime);
        stringBuilder.AppendLine(nameof(LinkRawFiles) + "=" + LinkRawFiles);
        stringBuilder.AppendLine(nameof(RawFilesFolderName) + "=" + RawFilesFolderName);
        stringBuilder.AppendLine(nameof(DeleteLinkedFilesOption) + "=" + DeleteLinkedFilesOption);
        stringBuilder.AppendLine(nameof(IncludeVideos) + "=" + IncludeVideos);
        stringBuilder.AppendLine(nameof(IsDebugLogEnabled) + "=" + IsDebugLogEnabled);
        return stringBuilder.ToString();
    }
}