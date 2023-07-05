using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tocronx.SimpleAsync;

namespace PhotoViewer.Core.ViewModels
{
    public abstract class MetadataPanelSectionModelBase : ViewModelBase
    {
        public bool IsWriting { get; private set; }

        public Task WriteFilesTask { get; private set; } = Task.CompletedTask;

        protected IReadOnlyList<IBitmapFileInfo> Files { get; private set; } = Array.Empty<IBitmapFileInfo>();

        private IList<MetadataView> metadata = Array.Empty<MetadataView>();

        private readonly IBackgroundTaskService backgroundTaskService;
        private readonly IDialogService dialogService;

        private protected MetadataPanelSectionModelBase(
            IMessenger messenger,
            IBackgroundTaskService backgroundTaskService, 
            IDialogService dialogService)
            : base(messenger) 
        {
            this.backgroundTaskService = backgroundTaskService;
            this.dialogService = dialogService;
        }

        public void UpdateFilesChanged(IReadOnlyList<IBitmapFileInfo> files, IList<MetadataView> metadata)
        {
            BeforeFilesChanged();
            Files = files;
            this.metadata = metadata;
            WriteFilesTask = Task.CompletedTask;
            IsWriting = false;
            OnFilesChanged(metadata);
        }

        public void UpdateMetadataModified(IMetadataProperty metadataProperty)
        {
            OnMetadataModified(metadata, metadataProperty);
        }

        protected virtual void BeforeFilesChanged() { }

        protected abstract void OnFilesChanged(IList<MetadataView> metadata);

        protected abstract void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty);

        protected async Task<ProcessingResult<IBitmapFileInfo>> WriteFilesAsync(Func<IBitmapFileInfo, Task> processFile)
        {
            IsWriting = true;

            var writeFilesTask = ExecuteWriteFilesAsync(Files.ToList(), processFile);

            WriteFilesTask = writeFilesTask;

            var result = await writeFilesTask;

            if (writeFilesTask.Id == WriteFilesTask.Id)
            {
                await RunInContextAsync(() => IsWriting = false).ConfigureAwait(false);
            }

            if (result.HasFailures)
            {
                await ShowWriteMetadataFailedDialog(result);
            }

            return result;
        }

        private Task<ProcessingResult<IBitmapFileInfo>> ExecuteWriteFilesAsync(
            IReadOnlyList<IBitmapFileInfo> files, Func<IBitmapFileInfo, Task> processFile)
        {
            var task = ParallelizationUtil.ProcessParallelAsync(files, async file =>
            {
                using (await file.AcquireExclusiveAccessAsync().ConfigureAwait(false))
                {
                    await processFile(file).ConfigureAwait(false);
                }
            });

            backgroundTaskService.RegisterBackgroundTask(task);

            return task;
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
