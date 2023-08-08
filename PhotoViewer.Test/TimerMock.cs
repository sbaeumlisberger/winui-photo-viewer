using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Test
{
    internal class TimerMock : ITimer
    {
        public bool IsEnabled { get; private set; }

        public event EventHandler<EventArgs>? Elapsed;

        public int RestartCount { get; private set; } = 0;

        public void Dispose()
        {

        }

        public void Restart()
        {
            RestartCount++;
            IsEnabled = true;
        }

        public void Start()
        {
            IsEnabled = true;
        }

        public void Stop()
        {
            IsEnabled = false;
        }

        public async Task RaiseElapsed()
        {
            IsEnabled = false;
            await Task.Run(() =>
            {
                Elapsed?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
