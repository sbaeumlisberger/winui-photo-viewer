using System.Globalization;

namespace PhotoViewerCore.Utils;

public static class StringUtils
{

    public static string JoinNonEmpty(string separator, params object?[] values)
    {
        return string.Join(separator, values.Where(x => !string.IsNullOrEmpty(x?.ToString())));
    }


    public static string ToInvariantString(this double d)
    {
        return d.ToString(CultureInfo.InvariantCulture);
    }
}
