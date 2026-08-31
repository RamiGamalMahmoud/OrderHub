using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace OrderHub.UI.Features.Confirm;

public partial class ConfirmDialogViewModel : ObservableObject
{
    public event Action<bool> ResultSubmitted;

    [ObservableProperty]
    private string _message;

    [RelayCommand]
    private void Confirm()
    {
        ResultSubmitted?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        ResultSubmitted?.Invoke(false);
    }
}