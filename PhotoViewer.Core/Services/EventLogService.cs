using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace PhotoViewer.Core.Services;

public class EventLogService
{
    private const string LastCheckTimestampKey = "EventLogLastCheckTimestamp";

    private readonly IPropertySet appData = AppData.DataContainer.Values;

    public List<string> GetErrors()
    {
        var errors = new List<string>();

        var lastCheckTimestamp = ((DateTimeOffset?)appData[LastCheckTimestampKey]) ?? DateTimeOffset.UtcNow;

        string query = $"*["
            + $"System/Level={(int)StandardEventLevel.Error}"
            + $" and System[TimeCreated[@SystemTime >= '{lastCheckTimestamp.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}']]"
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
