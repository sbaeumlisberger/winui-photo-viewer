using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

public class AsyncEventArgs
{

    private readonly List<Task> tasks = new List<Task>();

    public Task CompletionTask => Task.WhenAll(tasks);

    public void AddTask(Task task) 
    {
        tasks.Add(task);
    }

}
