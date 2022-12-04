using CommunityToolkit.Mvvm.Input;
using MetadataAPI;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.ViewModels;

public partial class DateTakenSectionModel : ViewModelBase
{
    public bool IsNotPresent { get; private set; }

    public bool IsSingleValue { get; private set; }

    public bool IsRange { get; private set; }

    public DateTimeOffset? Date { get; private set; }

    public TimeSpan? Time { get; private set; }

    public string RangeText { get; private set; } = "";

    public void Update(IList<MetadataView> metadata)
    {
        var values = metadata.Select(m => m.Get(MetadataProperties.DateTaken)).ToList();
        
        IsNotPresent = !values.Any(dateTaken => dateTaken is not null);
        IsSingleValue = !IsNotPresent && values.All(dateTaken => dateTaken == values.FirstOrDefault());
        IsRange = !IsNotPresent && !IsSingleValue;

        if (IsSingleValue)
        {
            var dateTaken = values.FirstOrDefault();
            Date = dateTaken;
            Time = dateTaken?.TimeOfDay;
        }
        else 
        {
            Date = null;
            Time = null;
        }

        if (IsRange)
        {
            var datesTaken = values.OfType<DateTime>().ToList();
            RangeText = FormatDate(datesTaken.Min()) + " - " + FormatDate(datesTaken.Max());
        }
        else 
        {
            RangeText = "";
        }
    }

    public void Clear()
    {
        IsNotPresent = true;
        IsSingleValue = false;
        IsRange = false;
        Date = null;
        Time = null;
        RangeText = "";
    }

    [RelayCommand]
    private void AddDateTaken() 
    {
        // TODO
    }

    [RelayCommand]
    private void ShiftDateTaken()
    { 
        // TODO
    }

    private string FormatDate(DateTimeOffset date)
    {
        return date.ToString("g", CultureInfo.InstalledUICulture);
    }
}
