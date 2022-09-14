using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewerCore.ViewModels
{
    [ObservableObject]
    public partial class MetadataStringViewModel
    {
        public bool HasMultipleValues 
        {
            get => _HasMultipleValues; 
            private set => SetProperty(ref _HasMultipleValues, value);
        }

        public string Value 
        { 
            get => _Value; 
            set 
            {
                SetProperty(ref _Value, value);
                OnValueChangedExternal();
            } 
        }

        private string _Value = string.Empty;
        private bool _HasMultipleValues = false;

        public MetadataStringViewModel() 
        {
        
        }

        public void SetValues(IEnumerable<string> values) 
        {
            if (values.Any())
            {
                HasMultipleValues = values.Any(v => !Equals(v, values.First()));
                _Value = HasMultipleValues ? string.Empty : values.First();
                OnPropertyChanged(nameof(Value));
            }
            else 
            {
                Clear();
            }
        }

        public void Clear() 
        {
            HasMultipleValues = false;
            _Value = string.Empty;
            OnPropertyChanged(nameof(Value));
        }

        /// <summary>
        /// e.g. Enter key pressed
        /// </summary>
        public void SignalTypingCompleted() 
        {
            ApplyValue(); // TODO
        }

        private void OnValueChangedExternal() 
        {
            // TODO defer (250ms)
            ApplyValue(); // TODO
        }

        private async Task ApplyValue() 
        {
            // cancel previous

            await Task.CompletedTask; // TODO

            HasMultipleValues = false;

            // handle erros
        }

    }
}
