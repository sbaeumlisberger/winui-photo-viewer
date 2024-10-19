using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoViewer.App.Utils;
using System;
using System.Linq;
using Windows.Foundation;

namespace PhotoViewer.App.Controls;

public sealed partial class TimePickerControl : UserControl
{

    public static readonly DependencyProperty TimeProperty = DependencyPropertyHelper<TimePickerControl>.Register<TimeSpan?>(nameof(Time), null, (s, e) => { s.TimePickerControl_TimeChanged(); });

    public event TypedEventHandler<TimePickerControl, EventArgs>? TimeChanged;

    public TimeSpan? Time { get => (TimeSpan?)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    public TimePickerControl()
    {
        InitializeComponent();
        hoursBox.ItemsSource = Enumerable.Range(0, 24).ToList();
        minutesBox.ItemsSource = Enumerable.Range(0, 60).ToList();
        secondsBox.ItemsSource = Enumerable.Range(0, 60).ToList();
    }

    private void TimePickerControl_TimeChanged()
    {
        hoursBox.SelectedValue = Time?.Hours;
        minutesBox.SelectedValue = Time?.Minutes;
        secondsBox.SelectedValue = Time?.Seconds;
        TimeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HoursBox_DropDownClosed(object sender, object args)
    {
        Time = new TimeSpan((int?)hoursBox.SelectedValue ?? 0, Time?.Minutes ?? 0, Time?.Seconds ?? 0);
    }

    private void MinutesBox_DropDownClosed(object sender, object args)
    {
        Time = new TimeSpan(Time?.Hours ?? 0, (int?)minutesBox.SelectedValue ?? 0, Time?.Seconds ?? 0);
    }

    private void SecondsBox_DropDownClosed(object sender, object args)
    {
        Time = new TimeSpan(Time?.Hours ?? 0, Time?.Minutes ?? 0, (int?)secondsBox.SelectedValue ?? 0);
    }

}
