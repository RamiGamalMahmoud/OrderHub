namespace OrderHub.UI.Features.Administration.Users.Create;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Features.Administration.Users.Create;
using OrderHub.Application.Interfaces;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Linq;
using System.Threading.Tasks;

public sealed class CreateUserViewModel : UserEditorViewModel
{
    private readonly IRequestExecutor _requestExecutor;

    public CreateUserViewModel(IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;
        IsReadOnly = false;
        ShowSaveButton = true;
        SaveButtonText = "إضافة المستخدم";
    }
    public override string Title => "إضافة مستخدم";
    public override bool ShowPassword => true;

    public override async Task SaveAsync()
    {
        if (!CanSave)
            return;

        try
        {
            IsSaving = true;
            var request = new CreateUserRequest(
                UserName, DisplayName, Password, IsActive, IsAdmin,
                Permissions.Select(x => new CreateUserRequestPermission(
                    x.Feature, x.CanRead, x.CanWrite)).ToList());

            var result = await _requestExecutor.ExecuteAsync(new CreateUserCommand(request));
            if (!result.IsSuccess)
            {
                NotificationService.Instance.ShowError(result.ErrorMessage);
                return;
            }
            
            NotificationService.Instance.ShowSuccess($"تم إضافة مستخدم جديد ({UserName})");

            Clear();
            
            WeakReferenceMessenger.Default.Send(new UsersMessages.UserCreatedMessage());
            
            IsReadOnly = true;
            ShowSaveButton = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void Clear()
    {
        DisplayName = string.Empty;
        UserName = string.Empty;

        Password = string.Empty;
        ConfirmPassword = string.Empty;

        IsActive = true;
        IsAdmin = false;

        foreach (var permission in Permissions)
        {
            permission.CanRead = false;
            permission.CanWrite = false;
        }

        IsAllSelected = false;

        ClearErrors();
        ValidateAllProperties();
    }
}
