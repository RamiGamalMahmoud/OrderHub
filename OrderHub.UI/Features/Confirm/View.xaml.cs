using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace OrderHub.UI.Features.Confirm
{
    [ObservableObject]
    public partial class View : Window
    {
        public View(string message)
        {
            InitializeComponent();

            DataContext = this;

            Message = message;
        }

        [ObservableProperty]
        private string _message;

        [RelayCommand]
        private void Confirm() => DialogResult = true;

        [RelayCommand]
        private void Cancel() => DialogResult = false;
    }
}
