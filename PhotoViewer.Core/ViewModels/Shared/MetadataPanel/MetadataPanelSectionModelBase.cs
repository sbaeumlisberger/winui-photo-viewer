using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using MetadataAPI;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PropertyChanged;
using System.Collections.Immutable;

namespace PhotoViewer.Core.ViewModels
{
    public abstract class MetadataPanelSectionModelBase : ViewModelBase
    {

        public bool IsWriting { get; private set; }

        [DoNotNotify]
        internal Task LastWriteFilesTask { get; private set; } = Task.CompletedTask;

        protected IImmutableList<IBitmapFileInfo> Files { get; private set; } = ImmutableList<IBitmapFileInfo>.Empty;

        private readonly IBackgroundTaskService backgroundTaskService;
        private readonly IDialogService dialogService;

        private readonly AsyncLock writeLock = new AsyncLock();

        private IReadOnlyList<MetadataView> metadata = Array.Empty<MetadataView>();

        private CancellationTokenSource? cancellationTokenSource;

        private protected MetadataPanelSectionModelBase(
            IMessenger messenger,
            IBackgroundTaskService backgroundTaskService,
            IDialogService dialogService)
            : base(messenger)
        {
            this.backgroundTaskService = backgroundTaskService;
            this.dialogService = dialogService;
        }

        public void UpdateFilesChanged(IImmutableList<IBitmapFileInfo> files, IReadOnlyList<MetadataView> metadata)
        {
            BeforeFilesChanged();
            Files = files;
            IsWriting = false;
            cancellationTokenSource = null;
            this.metadata = metadata;
            OnFilesChanged(metadata);
        }

        public void UpdateMetadataModified(IMetadataProperty metadataProperty)
        {
            OnMetadataModified(metadata, metadataProperty);
        }

        protected virtual void BeforeFilesChanged() { }

        protected abstract void OnFilesChanged(IReadOnlyList<MetadataView> metadata);

        protected abstract void OnMetadataModified(IReadOnlyList<MetadataView> metadata, IMetadataProperty metadataProperty);

        protected async Task<bool> WriteFilesAsync(
            Func<IBitmapFileInfo, Task> processFile,
            Action<IReadOnlyList<IBitmapFileInfo>> onComplete)
        {
            return await WriteFilesAsync((element, cancellationToken) => processFile(element), onComplete);
        }

        protected async Task<bool> WriteFilesAndCancelPreviousAsync(
            Func<IBitmapFileInfo, CancellationToken, Task> processFile,
            Action<IReadOnlyList<IBitmapFileInfo>> onComplete)
        {
            cancellationTokenSource?.Cancel();
            return await WriteFilesAsync(processFile, onComplete);
        }

        protected async Task<bool> WriteFilesAsync(
             Func<IBitmapFileInfo, CancellationToken, Task> processFile,
             Action<IReadOnlyList<IBitmapFileInfo>> onComplete)
        {
            var task = ExecuteWriteFilesAsync(Files, processFile, onComplete);
            LastWriteFilesTask = task;
            backgroundTaskService.RegisterBackgroundTask(task);
            return await task;
        }

        private async Task<bool> ExecuteWriteFilesAsync(
            IImmutableList<IBitmapFileInfo> files,
            Func<IBitmapFileInfo, CancellationToken, Task> processFile,
            Action<IReadOnlyList<IBitmapFileInfo>> onComplete)
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            using (await writeLock.AcquireAsync())
            {
                try
                {
                    if (ReferenceEquals(files, Files))
                    {
                        await DispatchAsync(() => IsWriting = true);
                    }

                    var result = await files.Parallel(cancellationToken).TryProcessAsync(processFile);

                    onComplete(result.ProcessedElements);

                    if (result.HasFailures)
                    {
                        await ShowWriteMetadataFailedDialog(result);
                    }

                    return result.IsSuccessfully;
                }
                catch (OperationCanceledException)
                {
                    // canceled
                    return false;
                }
                catch (Exception exception)
                {
                    Log.Error("Error while writing files", exception);
                    // TODO:
                    return false;
                }
                finally
                {
                    if (ReferenceEquals(files, Files))
                    {
                        await DispatchAsync(() => IsWriting = false);
                    }
                }
            }
        }

        private async Task ShowWriteMetadataFailedDialog(ParallelResult<IBitmapFileInfo> processingResult)
        {
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.WriteMetadataFailedDialog_Title,
                Message = string.Join("\n", processingResult.Failures.Select(failure => failure.Element.FileName + ": " + failure.Exception.Message))
            });
        }
    }
}
