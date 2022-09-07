namespace PhotoViewerApp.Utils;

public static class ByteSizeFormatter
{
    private static readonly string[] sizes = { "kB", "MB", "GB", "TB", "PB" };

    /// <summary>
    /// Formats a size in bytes as a readable string. Divides by 1024.
    /// </summary>
    /// <param name="size">The size to format in bytes</param>
    /// <param name="decimals">The number of decimals used if the size is greater than 1024 Bytes</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information</param>
    /// <returns>A formatted string</returns>
    public static string Format(ulong size, int decimals = 2, IFormatProvider? formatProvider = null)
    {
        int order = 0;
        while (size >= 1024 * 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        if (size >= 1024)
        {
            string numberFormat = "0." + new string(Enumerable.Repeat('0', decimals).ToArray());
            return (size / 1024d).ToString(numberFormat, formatProvider) + " " + sizes[order];
        }
        return size + " " + (size > 1 ? "Bytes" : "Byte");
    }

}