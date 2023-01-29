using System;
using System.Threading.Tasks;

namespace PhotoViewer.App.Utils;

// TODO implement thread-safe
public class Progress : IProgress<int>
{

    public int Target { get; private set; }

    public int Count { get; private set; } = 0;

    public void Initialize(bool canCancel, bool canPause, int target)
    {

    }

    public bool Report(int increment)
    {
        return false;
    }

    void IProgress<int>.Report(int increment)
    {

    }

    public void Fail() 
    {
    
    }

    // return false when canceled of failed
    public Task<bool> WaitIfPausedAndReport(int increment)
    {
        return Task.FromResult(false);
    }

}