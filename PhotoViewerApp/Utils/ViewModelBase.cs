using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerApp.Utils
{
    public class ViewModelBase : ObservableObject
    {
        private IMessenger messenger;

        private IDialogService dialogService;

        public ViewModelBase(IMessenger? messenger = null, IDialogService? dialogService = null)
        {
            this.messenger = messenger ?? Messenger.GetForCurrentThread();
            this.dialogService = dialogService ?? DialogService.GetForCurrentWindow();
        }

        protected void Publish<T>(T message) where T : notnull
        {
            messenger.Publish<T>(message);
        }

        protected void Subscribe<T>(Action<T> callback) where T : notnull
        {
            messenger.Subscribe<T>(callback);
        }

        protected async Task ShowDialogAsync(object dialogModel) 
        {
            await dialogService.ShowDialogAsync(dialogModel);
        }

    }
}
