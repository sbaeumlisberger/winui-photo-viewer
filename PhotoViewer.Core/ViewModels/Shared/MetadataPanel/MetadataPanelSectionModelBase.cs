using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoViewer.Core.ViewModels
{
    public abstract class MetadataPanelSectionModelBase : ViewModelBase
    {

        public bool IsWriting { get; private set; }

        internal Task WriteFilesTask => writeLock.AcquireAsync().ContinueWith(task => task.Result.Dispose());

        protected IImmutableList<IBitmapFileInfo> Files { get; private set; } = ImmutableList<IBitmapFileInfo>.Empty;

        private readonly IBackgroundTaskService backgroundTaskService;
        private readonly IDialogService dialogService;

        private readonly AsyncLockFIFO writeLock = new AsyncLockFIFO();

        private IList<MetadataView> metadata = Array.Empty<MetadataView>();

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

        public void UpdateFilesChanged(IImmutableList<IBitmapFileInfo> files, IList<MetadataView> metadata)
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

        protected abstract void OnFilesChanged(IList<MetadataView> metadata);

        protected abstract void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty);

        protected async Task<ProcessingResult<IBitmapFileInfo>> WriteFilesAsync(Func<IBitmapFileInfo, Task> processFile, bool cancelPrevious = false)
        {
            var task = WriteFilesAsync(Files, processFile, cancelPrevious);
            backgroundTaskService.RegisterBackgroundTask(task);
            return await task;
        }

        private async Task<ProcessingResult<IBitmapFileInfo>> WriteFilesAsync(
            IImmutableList<IBitmapFileInfo> files, Func<IBitmapFileInfo, Task> processFile, bool cancelPrevious = false)
        {
            if (cancelPrevious)
            {
                cancellationTokenSource?.Cancel();
            }
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            using (await writeLock.AcquireAsync())
            {
                return await ExecuteWriteFilesAsync(files, processFile, cancellationToken);
            }
        }

        private async Task<ProcessingResult<IBitmapFileInfo>> ExecuteWriteFilesAsync(
            IImmutableList<IBitmapFileInfo> files, Func<IBitmapFileInfo, Task> processFile, CancellationToken cancellationToken)
        {
            if (ReferenceEquals(files, Files))
            {
                await RunInContextAsync(() => IsWriting = true);
            }

            var task = ParallelizationUtil.ProcessParallelAsync(files, async file =>
            {
                await processFile(file).ConfigureAwait(false);
            },
            cancellationToken: cancellationToken);

            var result = await task;

            if (ReferenceEquals(files, Files))
            {
                await RunInContextAsync(() => IsWriting = false);
            }

            if (result.HasFailures && !cancellationToken.IsCancellationRequested)
            {
                await ShowWriteMetadataFailedDialog(result);
            }

            return result;
        }

        private async Task ShowWriteMetadataFailedDialog(ProcessingResult<IBitmapFileInfo> processingResult)
        {
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.WriteMetadataFailedDialog_Title,
                Message = string.Join("\n", processingResult.Failures.Select(failure => failure.Element.FileName + ": " + failure.Exception.Message))
            });
        }
    }
}
