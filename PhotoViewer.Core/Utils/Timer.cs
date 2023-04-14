using PhotoViewer.App.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PhotoViewer.Core.Utils;

internal delegate ITimer TimerFactory(TimeSpan interval, bool autoRestart = true);

internal interface ITimer : IDisposable
{
    event EventHandler<EventArgs> Elapsed;

    bool IsEnabled { get; }

    void Start();

    void Stop();

    void Restart();
}

internal class Timer : ITimer
{
    public event EventHandler<EventArgs>? Elapsed;

    public bool IsEnabled => timer.Enabled;

    private readonly System.Timers.Timer timer;

    private Timer(TimeSpan interval, bool autoRestart)
    {
        timer = new System.Timers.Timer(interval) { AutoReset = autoRestart };
        timer.Elapsed += Timer_Elapsed;
    }

    public static ITimer Create(TimeSpan interval, bool autoRestart) 
    {
        return new Timer(interval, autoRestart);
    }

    public void Dispose()
    {
        timer.Dispose();
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
       Elapsed?.Invoke(this, e);
    }

    public void Stop()
    {
        timer.Stop();
    }

    public void Start()
    {
        timer.Start();
    }

    public void Restart()
    {
        timer.Stop();
        timer.Start();
    }

}
