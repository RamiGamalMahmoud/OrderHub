using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Features.Administration.Users.ActivateUser;
using OrderHub.Application.Features.Administration.Users.DeactivateUser;
using OrderHub.Application.Features.Administration.Users.GetUsers;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Administration.Users.List;

public partial class UsersViewModel : ObservableObject
{
    private readonly IRequestExecutor _requestExecutor;
    private bool _reloadRequested;

    [ObservableProperty]
    private IReadOnlyList<UserListItem> _users = [];

    [ObservableProperty]
    private bool _isLoading;

    public UsersViewModel(IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;

        WeakReferenceMessenger.Default.Register<UsersViewModel, UsersMessages.UserCreatedMessage>(
            this, async (recipient, _) => await recipient.LoadAsync());

        WeakReferenceMessenger.Default.Register<UsersViewModel, UsersMessages.UserUpdatedMessage>(
            this, async (recipient, _) => await recipient.LoadAsync());

        WeakReferenceMessenger.Default.Register<UsersViewModel, UsersMessages.UserActivatedMessage>(
            this, async (recipient, _) => await recipient.LoadAsync());

        WeakReferenceMessenger.Default.Register<UsersViewModel, UsersMessages.UserDeactivatedMessage>(
            this, async (recipient, _) => await recipient.LoadAsync());
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            _reloadRequested = true;
            return;
        }

        try
        {
            IsLoading = true;

            do
            {
                _reloadRequested = false;
                Users = await _requestExecutor.ExecuteAsync(new GetUsersQuery());
            }
            while (_reloadRequested);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddUser() => await DialogService.Instance.ShowDialog<Create.CreateUserView>("إضافة مستخدم");


    [RelayCommand]
    private async Task EditUser(int id) => await DialogService.Instance.ShowDialog<Details.UserDetailsView>("", id);

    [RelayCommand]
    private async Task ToggleUserStatus(UserListItem user)
    {
        if (user is null)
            return;

        bool activate = !user.IsActive;

        Result result = activate
            ? await _requestExecutor.ExecuteAsync(
                new ActivateUserCommand(user.Id))
            : await _requestExecutor.ExecuteAsync(
                new DeactivateUserCommand(user.Id));

        if (!result.IsSuccess)
        {
            NotificationService.Instance.ShowError(
                result.ErrorMessage);

            return;
        }

        if (activate)
        {
            WeakReferenceMessenger.Default.Send(
                new UsersMessages.UserActivatedMessage(user.Id));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(
                new UsersMessages.UserDeactivatedMessage(user.Id));
        }

        NotificationService.Instance.ShowSuccess(
            activate
                ? "تم تفعيل المستخدم بنجاح."
                : "تم تعطيل المستخدم بنجاح.");
    }

    [ObservableProperty]
    private UserListItem _selectedUser;
}
