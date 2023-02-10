using System;

namespace PhotoVieweApp.Utils;

public static class TimeSpanFormatter
{
    public static string Format(TimeSpan timeSpan)
    {
        if (timeSpan.TotalMinutes < 1)
        {
            return "< 1min";
        }

        string formattedString = "";

        if (timeSpan.Hours > 0)
        {
            formattedString += timeSpan.Hours + "h ";
        }

        if (timeSpan.Minutes > 0)
        {
            formattedString += timeSpan.Minutes + "min ";
        }

        return formattedString.TrimEnd(' ');
    }

}
