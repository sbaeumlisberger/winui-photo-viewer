namespace PhotoViewerCore.Utils;

internal class StringUtils
{

    public static string JoinNonEmpty(string separator, params object?[] values) 
    {
        return string.Join(separator, values.Where(x => !string.IsNullOrEmpty(x?.ToString())));
    }

}
