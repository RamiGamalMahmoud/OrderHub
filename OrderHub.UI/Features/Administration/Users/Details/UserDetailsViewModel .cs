using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Features.Administration.Users.GetUser;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.UI.Messaging;
using OrderHub.Application.Features.Administration.Users.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using System.Linq;
using System.Threading.Tasks;
using OrderHub.UI.Services;

namespace OrderHub.UI.Features.Administration.Users.Details;
public sealed partial class UserDetailsViewModel
    : UserEditorViewModel, IParameterizedViewModel
{
    private readonly IRequestExecutor _requestExecutor;

    private int _id;

    public UserDetailsViewModel(IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;

        IsReadOnly = true;
        ShowSaveButton = false;
        SaveButtonText = string.Empty;
    }

    public override string Title => "بيانات المستخدم";

    public override bool ShowPassword => false;

    [RelayCommand]
    private void Edit()
    {
        IsReadOnly = false;
        ShowSaveButton = true;
        SaveButtonText = "حفظ التعديلات";
    }

    public async Task Initialize(object parameter)
    {
        if (parameter is not int id)
            return;

        _id = id;

        var user = await _requestExecutor.ExecuteAsync(
            new GetUserQuery(_id));

        UserName = user.UserName;
        DisplayName = user.DisplayName;
        IsActive = user.IsActive;
        IsAdmin = user.IsAdmin;

        foreach (var permission in user.Permissions)
        {
            var item = Permissions.FirstOrDefault(
                x => x.Feature == permission.PermissionFeature);

            if (item is null)
                continue;

            item.CanRead = permission.CanRead;
            item.CanWrite = permission.CanWrite;
        }
    }

    public override async Task SaveAsync()
    {
        if (!CanSave || _id <= 0)
            return;

        try
        {
            IsSaving = true;
            var permissions = Permissions
                .Select(x => new UpdateUserRequestPermission(
                    x.Feature,
                    x.CanRead,
                    x.CanWrite))
                .ToList();

            Result result = await _requestExecutor.ExecuteAsync(
                new UpdateUserCommand(
                    _id,
                    new UpdateUserRequest(
                        UserName,
                        DisplayName,
                        IsActive,
                        IsAdmin,
                        permissions)));

            if (!result.IsSuccess)
            {
                NotificationService.Instance.ShowError(result.ErrorMessage);
                return;
            }

            NotificationService.Instance.ShowSuccess("تم تعديل بيانات المستخدم الحالي.");

            WeakReferenceMessenger.Default.Send(new UsersMessages.UserUpdatedMessage(_id));
            IsReadOnly = true;
            ShowSaveButton = false;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
