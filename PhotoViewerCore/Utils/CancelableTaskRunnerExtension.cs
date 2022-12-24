using PostSharp.Aspects.Advices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tocronx.SimpleAsync;

namespace PhotoViewerCore.Utils
{
    internal static class CancelableTaskRunnerExtension
    {

        public static void Cancel(this CancelableTaskRunner cancelableTaskRunner) 
        {
            cancelableTaskRunner.RunAndCancelPrevious(cancellationToken => Task.CompletedTask);
        }
    }
}
