using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Windows.Input;
using Tocronx.SimpleAsync;
using Windows.Storage;

namespace PhotoViewer.Core.Commands;

internal interface IMoveRawFilesToSubfolderCommand : ICommand { }

internal class MoveRawFilesToSubfolderCommand : AsyncCommandBase, IMoveRawFilesToSubfolderCommand
{
    private readonly ApplicationSession session;

    private readonly ApplicationSettings settings;

    private readonly IDialogService dialogService;

    public MoveRawFilesToSubfolderCommand(ApplicationSession session, ApplicationSettings settings, IDialogService dialogService) 
    {
        this.session = session;
        this.settings = settings;
        this.dialogService = dialogService;
}

    protected override async Task ExecuteAsync()
    {
        var cts = new CancellationTokenSource();
        var progress = new Progress(cts);

        var progressDialogModel = new ProgressDialogModel()
        {
            Title = Strings.MoveRawFilesToSubfolderDialog_Title,
            Message = Strings.MoveRawFilesToSubfolderDialog_InProgressMessage,
            Progress = progress
        };
        dialogService.ShowDialogAsync(progressDialogModel).FireAndForget();

        try
        {
            var notMovedFiles = await MoveFilesAsync(session.Files, progress, cts.Token);                  

            if (notMovedFiles.Any())
            {
                progress.Fail();
                progressDialogModel.Message = Strings.MoveRawFilesToSubfolderDialog_ErrorMessage
                 + Environment.NewLine
                 + string.Join(Environment.NewLine, notMovedFiles);            
            }
            else
            {
                progressDialogModel.Message = Strings.MoveRawFilesToSubfolderDialog_SuccessMessage;
            }
        }
        catch (OperationCanceledException)
        {
            Log.Info("Moving RAW files to subfolder was canceled");
        }
        catch (Exception ex)
        {
            Log.Error("Moving RAW files to subfolder has failed", ex);
            progress.Fail();
            progressDialogModel.Message = ex.Message;
        }
    }

    private async Task<List<string>> MoveFilesAsync(IReadOnlyCollection<IMediaFileInfo> files, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var filesToMove = new List<IStorageFile>();
        foreach (var mediaFile in files)
        {
            foreach (var storageFile in mediaFile.StorageFiles)
            {
                if (BitmapFileInfo.RawFileExtensions.Contains(storageFile.FileType.ToLower())
                    && Path.GetDirectoryName(storageFile.Path) is string directoryPath
                    && !directoryPath.EndsWith("/" + settings.RawFilesFolderName))
                {
                    filesToMove.Add(storageFile);
                }
            }
        }

        if (!filesToMove.Any())
        {
            progress.Report(1);
            return new List<string>();
        }

        var folder = await filesToMove.First().GetParentAsync();
        var rawFilesFolder = await folder.CreateFolderAsync(settings.RawFilesFolderName, CreationCollisionOption.OpenIfExists);
        cancellationToken.ThrowIfCancellationRequested();
        
        var notMovedFiles = new List<string>();
        int count = 0;
        foreach (var file in filesToMove)
        {
            try
            {
                await file.MoveAsync(rawFilesFolder);
            }
            catch
            {
                notMovedFiles.Add(file.Name);
            }
            cancellationToken.ThrowIfCancellationRequested();
            count++;
            progress.Report(count / filesToMove.Count);
        }
        progress.Report(1);
        return notMovedFiles;
    }
}
