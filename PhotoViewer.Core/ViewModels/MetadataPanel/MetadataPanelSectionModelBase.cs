using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Resources;
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

        public Task WriteTask => writeFilesRunner.ExecutionTask;

        protected IReadOnlyList<IBitmapFileInfo> Files { get; private set; } = Array.Empty<IBitmapFileInfo>();

        private readonly SequentialTaskRunner writeFilesRunner;

        private IList<MetadataView> metadata = Array.Empty<MetadataView>();

        protected MetadataPanelSectionModelBase(SequentialTaskRunner writeFilesRunner, IMessenger messenger)
            : base(messenger)
        {
            this.writeFilesRunner = writeFilesRunner;
        }

        public void UpdateFilesChanged(IReadOnlyList<IBitmapFileInfo> files, IList<MetadataView> metadata)
        {
            BeforeFilesChanged();
            Files = files;
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

        protected Task EnqueueWriteFiles(Func<IReadOnlyList<IBitmapFileInfo>, Task> writeFiles)
        {
            var files = Files.ToList();

            return writeFilesRunner.Enqueue(async () =>
            {
                await RunOnUIThreadAsync(() => IsWriting = true).ConfigureAwait(false);

                await writeFiles(files).ConfigureAwait(false);

                if (writeFilesRunner.IsEmpty)
                {
                    await RunOnUIThreadAsync(() => IsWriting = false).ConfigureAwait(false);
                }
            });
        }

        protected async Task ShowWriteMetadataFailedDialog(IDialogService dialogService, ProcessingResult<IBitmapFileInfo> processingResult) 
        {
            await dialogService.ShowDialogAsync(new MessageDialogModel()
            {
                Title = Strings.WriteMetadataFailedDialog_Title,
                Message = string.Join("\n", processingResult.Failures.Select(failure => failure.Element.FileName + ": " + failure.Exception.Message))
            });
        }

    }
}
