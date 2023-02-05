using CommunityToolkit.Mvvm.Messaging;
using MetadataAPI;
using PhotoViewer.App.Models;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
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
            Files = files;
            this.metadata = metadata;
            OnFilesChanged(metadata);
        }

        public void UpdateMetadataModified(IMetadataProperty metadataProperty)
        {
            OnMetadataModified(metadata, metadataProperty);
        }

        protected abstract void OnFilesChanged(IList<MetadataView> metadata);

        protected abstract void OnMetadataModified(IList<MetadataView> metadata, IMetadataProperty metadataProperty);

        protected Task EnqueueWriteFiles(Func<IReadOnlyList<IBitmapFileInfo>, Task> writeFiles)
        { 
            return writeFilesRunner.Enqueue(() => writeFiles(Files.ToList()));
        }

    }
}
