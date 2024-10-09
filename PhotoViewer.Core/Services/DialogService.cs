using Essentials.NET;

namespace PhotoViewer.Core.Services;

public class DialogRequestedEventArgs : AsyncEventArgs
{
    public object DialogModel { get; }

    public DialogRequestedEventArgs(object dialogModel)
    {
        DialogModel = dialogModel;
    }
}

internal interface IDialogService
{
    event EventHandler<DialogRequestedEventArgs> DialogRequested;

    Task ShowDialogAsync(object dialogModel);
}


internal class DialogService : IDialogService
{
    public event EventHandler<DialogRequestedEventArgs>? DialogRequested;

    public async Task ShowDialogAsync(object dialogModel)
    {
        var eventArgs = new DialogRequestedEventArgs(dialogModel);
        DialogRequested?.Invoke(this, eventArgs);
        await eventArgs.CompletionTask;
    }
}
