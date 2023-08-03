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
        private class Session : ObservableObjectBase
        {
            public bool IsWriting { get; private set; }

            private readonly IImmutableList<IBitmapFileInfo> files;

            private readonly AsyncLockFIFO writeLock;

            private readonly IDialogService dialogService;

            private CancellationTokenSource? cancellationTokenSource;

            public Session(IImmutableList<IBitmapFileInfo> files, AsyncLockFIFO writeLock, IDialogService dialogService)
            {
                this.files = files;
                this.writeLock = writeLock;
                this.dialogService = dialogService;
            }

            public async Task<ProcessingResult<IBitmapFileInfo>> WriteFilesAsync(Func<IBitmapFileInfo, Task> processFile, bool cancelPrevious = false)
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
                    return await ExecuteWriteFilesAsync(processFile, cancellationToken);
                }
            }

            private async Task<ProcessingResult<IBitmapFileInfo>> ExecuteWriteFilesAsync(Func<IBitmapFileInfo, Task> processFile, CancellationToken cancellationToken)
            {
                IsWriting = true;

                var task = ParallelizationUtil.ProcessParallelAsync(files, async file =>
                {
                    await processFile(file).ConfigureAwait(false);
                },
                cancellationToken: cancellationToken);

                var result = await task;

                IsWriting = false;

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

        public bool IsWriting { get; private set; }

        internal Task WriteFilesTask => writeLock.AcquireAsync().ContinueWith(task => task.Result.Dispose());

        protected IReadOnlyList<IBitmapFileInfo> Files { get; private set; } = Array.Empty<IBitmapFileInfo>();

        private IList<MetadataView> metadata = Array.Empty<MetadataView>();

        private readonly AsyncLockFIFO writeLock = new AsyncLockFIFO();

        private readonly IBackgroundTaskService backgroundTaskService;
        private readonly IDialogService dialogService;

        private Session? session;

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
            this.metadata = metadata;
            session?.UnsubscribeAll(this);
            session = new Session(files, writeLock, dialogService);
            session.Subscribe(this, nameof(Session.IsWriting), () => IsWriting = session.IsWriting, initialCallback: true);
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
            var task = session!.WriteFilesAsync(processFile, cancelPrevious);
            backgroundTaskService.RegisterBackgroundTask(task);
            return await task;
        }
    }
}
