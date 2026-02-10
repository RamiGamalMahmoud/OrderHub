using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OrderHub.UI.Common
{
    public abstract partial class EditorViewModelBase : ObservableValidator
    {
        public event Action RequestClose;
        public virtual bool CanSave => HasChanges && !HasErrors;
        public abstract string Title { get; }

        protected List<string> _notifyPropertiesNames = new List<string>();

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (_notifyPropertiesNames.Contains(e.PropertyName))
            {
                HasChanges = true;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        protected abstract Task Save();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool _hasChanges;

        [RelayCommand]
        private void CloseEditor() => OnRequestClose();

        protected void OnRequestClose() => RequestClose?.Invoke();

    }
}
