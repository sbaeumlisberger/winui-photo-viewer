using System.Diagnostics.Eventing.Reader;
using Windows.Foundation.Collections;

namespace PhotoViewer.Core.Services;

public interface IEventLogService
{
    List<string> GetErrorsSinceLastCheck();
}

public class EventLogService : IEventLogService
{
    private const string LastCheckTimestampKey = "EventLogLastCheckTimestamp";

    private readonly IPropertySet appData = AppData.DataContainer.Values;

    public List<string> GetErrorsSinceLastCheck()
    {
        var errors = new List<string>();

        var lastCheckTimestamp = ((DateTimeOffset?)appData[LastCheckTimestampKey]) ?? DateTimeOffset.UtcNow;

        string query = $"*["
            + $"System/Level={(int)StandardEventLevel.Error}"
            + $" and System[TimeCreated[@SystemTime >= '{lastCheckTimestamp.UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}']]"
            + "]";

        var eventLogQuery = new EventLogQuery("Application", PathType.LogName, query) { ReverseDirection = true };

        var eventLogReader = new EventLogReader(eventLogQuery);

        for (EventRecord eventRecord; (eventRecord = eventLogReader.ReadEvent()) != null;)
        {
            string description = eventRecord.FormatDescription();

            if (description != null && description.Contains(AppData.ExecutableName))
            {
                errors.Add(description);
            }
        }

        appData[LastCheckTimestampKey] = DateTimeOffset.UtcNow;

        return errors;
    }

}
