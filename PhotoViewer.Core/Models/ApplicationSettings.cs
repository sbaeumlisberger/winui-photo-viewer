using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace PhotoViewer.Core.Models;

public partial class ApplicationSettings : ObservableObject
{
    public static readonly TimeSpan DefaultDiashowTime = TimeSpan.FromSeconds(3);

    public AppTheme Theme { get; set; } = AppTheme.System;
    public bool ShowDeleteAnimation { get; set; } = true;
    public bool AutoOpenMetadataPanel { get; set; } = false;
    public bool AutoOpenDetailsBar { get; set; } = false;
    public TimeSpan DiashowTime { get; set; } = DefaultDiashowTime;

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

    public static ApplicationSettings Deserialize(IEnumerable<string> serialized)
    {
        var settings = new ApplicationSettings();
        foreach (string line in serialized)
        {
            int equlasSignIndex = line.IndexOf('=');
            if (equlasSignIndex != -1)
            {
                var key = line.AsSpan(0, equlasSignIndex).Trim();
                var value = line.AsSpan(equlasSignIndex + 1).Trim();

                switch (key)
                {
                    case nameof(Theme):
                        settings.Theme = Enum.Parse<AppTheme>(value);
                        break;
                    case nameof(ShowDeleteAnimation):
                        settings.ShowDeleteAnimation = bool.Parse(value);
                        break;
                    case nameof(AutoOpenMetadataPanel):
                        settings.AutoOpenMetadataPanel = bool.Parse(value);
                        break;
                    case nameof(AutoOpenDetailsBar):
                        settings.AutoOpenDetailsBar = bool.Parse(value);
                        break;
                    case nameof(DiashowTime):
                        settings.DiashowTime = TimeSpan.ParseExact(value, "hh\\:mm\\:ss", null);
                        break;
                    case nameof(LinkRawFiles):
                        settings.LinkRawFiles = bool.Parse(value);
                        break;
                    case nameof(RawFilesFolderName):
                        settings.RawFilesFolderName = value.ToString();
                        break;
                    case nameof(DeleteLinkedFilesOption):
                        settings.DeleteLinkedFilesOption = Enum.Parse<DeleteLinkedFilesOption>(value);
                        break;
                    case nameof(IncludeVideos):
                        settings.IncludeVideos = bool.Parse(value);
                        break;
                    case nameof(IsDebugLogEnabled):
                        settings.IsDebugLogEnabled = bool.Parse(value);
                        break;
                }
            }
        };
        return settings;
    }

    public string Serialize()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(nameof(Theme) + "=" + Theme);
        stringBuilder.AppendLine(nameof(ShowDeleteAnimation) + "=" + ShowDeleteAnimation);
        stringBuilder.AppendLine(nameof(AutoOpenMetadataPanel) + "=" + AutoOpenMetadataPanel);
        stringBuilder.AppendLine(nameof(AutoOpenDetailsBar) + "=" + AutoOpenDetailsBar);
        stringBuilder.AppendLine(nameof(DiashowTime) + "=" + DiashowTime.ToString("hh\\:mm\\:ss"));
        stringBuilder.AppendLine(nameof(LinkRawFiles) + "=" + LinkRawFiles);
        stringBuilder.AppendLine(nameof(RawFilesFolderName) + "=" + RawFilesFolderName);
        stringBuilder.AppendLine(nameof(DeleteLinkedFilesOption) + "=" + DeleteLinkedFilesOption);
        stringBuilder.AppendLine(nameof(IncludeVideos) + "=" + IncludeVideos);
        stringBuilder.AppendLine(nameof(IsDebugLogEnabled) + "=" + IsDebugLogEnabled);
        return stringBuilder.ToString();
    }
}