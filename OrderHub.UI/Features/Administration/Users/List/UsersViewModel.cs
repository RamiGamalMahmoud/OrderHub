using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Features.Administration.Users.GetUsers;
using OrderHub.Application.Interfaces;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Administration.Users.List;

public partial class UsersViewModel : ObservableObject
{
    private readonly IRequestExecutor _requestExecutor;

    [ObservableProperty]
    private IReadOnlyList<UserListItem> _users = [];

    [ObservableProperty]
    private bool _isLoading;

    public UsersViewModel(IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;

            Users = await _requestExecutor.ExecuteAsync(
                new GetUsersQuery());
        }
        finally
        {
            IsLoading = false;
        }
    }
}