using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace PhotoViewer.Test
{
    internal class SynchronizationContextMock : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
            using var _ = Apply();
            callback(state);
        }

        public IDisposable Apply()
        {
            var oldSyncContext = Current;
            SetSynchronizationContext(this);
            return new DisposableAction(() => SetSynchronizationContext(oldSyncContext));
        }
    }
}
