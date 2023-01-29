using System.Text;
using System.Text.Json;

namespace PhotoViewer.App.Utils.Logging;

internal static class ExceptionFormatter
{

    public static readonly string IndentStep = "  ";

    public static string Format(Exception exception)
    {
        StringBuilder stringBuilder = new StringBuilder();
        AppendException(stringBuilder, exception, "");
        return stringBuilder.ToString();
    }

    private static void AppendException(StringBuilder stringBuilder, Exception exception, string indent)
    {
        stringBuilder.Append(exception.GetType());
        stringBuilder.Append(": ");
        stringBuilder.Append(exception.Message);

        AppendStackTrace(stringBuilder, exception, indent);

        AppendProperties(stringBuilder, exception, indent);

        if (exception is AggregateException aggregateException)
        {
            AppendInnerExceptions(stringBuilder, aggregateException.InnerExceptions, indent);
        }
        else if (exception.InnerException is not null)
        {
            AppendInnerException(stringBuilder, exception.InnerException, indent);
        }

        stringBuilder.AppendLine();
    }
    private static void AppendStackTrace(StringBuilder stringBuilder, Exception exception, string indent)
    {
        if (exception.StackTrace is string stackTrace)
        {
            foreach (string line in stackTrace.Split("\n"))
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(indent);
                stringBuilder.Append(IndentStep);
                stringBuilder.Append(line.Trim());
            }
        }
    }

    private static void AppendProperties(StringBuilder stringBuilder, Exception exception, string indent)
    {
        foreach (var property in exception.GetType().GetProperties())
        {
            if (property.Name == "Message" || property.Name == "StackTrace" || property.Name == "InnerException")
            {
                continue;
            }
            var propertyValue = property.GetValue(exception);
            stringBuilder.AppendLine();
            stringBuilder.Append(indent);
            stringBuilder.Append(IndentStep);
            stringBuilder.Append(property.Name);
            stringBuilder.Append(": ");
            try
            {
                stringBuilder.Append(JsonSerializer.Serialize(propertyValue));
            }
            catch
            {
                stringBuilder.Append(propertyValue);
            }
        }
    }

    private static void AppendInnerException(StringBuilder stringBuilder, Exception innerException, string indent)
    {
        stringBuilder.AppendLine();
        stringBuilder.Append(indent);
        stringBuilder.Append("InnerException: ");
        AppendException(stringBuilder, innerException, indent);
    }

    private static void AppendInnerExceptions(StringBuilder stringBuilder, IReadOnlyList<Exception> innerExceptions, string indent)
    {
        stringBuilder.AppendLine();
        stringBuilder.Append(indent);
        stringBuilder.Append("InnerExceptions:");
        for (int i = 0; i < innerExceptions.Count; i++)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append(indent);
            stringBuilder.Append("[");
            stringBuilder.Append(i);
            stringBuilder.Append("] ");
            AppendException(stringBuilder, innerExceptions[i], indent + IndentStep);
        }
    }

}
