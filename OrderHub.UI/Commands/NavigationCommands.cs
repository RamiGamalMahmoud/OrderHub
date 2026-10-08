using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.UI.Services;
using System;

namespace OrderHub.UI.Commands;

public partial class NavigationCommands : ObservableObject, ICommandGroup
{
    public NavigationCommands()
    {
        NavigationService.Instance.NavigationChanged += OnNavigationChanged;
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => NavigationService.Instance.GoBack();

    private bool CanGoBack => NavigationService.Instance.CanGoBack;

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward() => NavigationService.Instance.GoForward();

    private bool CanGoForward => NavigationService.Instance.CanGoForward;

    private void OnNavigationChanged(object sender, EventArgs e)
    {
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
    }
}