using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.UI.Features.Administration.Users.Create;
using OrderHub.UI.Features.Administration.Users.Details;
using OrderHub.UI.Features.Administration.Users.Shared;
using OrderHub.UI.Features.Administration.Users.Update;
using System.Linq;

namespace OrderHub.Tests.UI;

public class UserEditorViewModelTests
{
    [Fact]
    public void CreateRequiresCommonFieldsAndMatchingPasswords()
    {
        var editor = new CreateUserViewModel(Moq.Mock.Of<IRequestExecutor>());
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.Contains(editor.GetErrors(nameof(editor.DisplayName)), e => e.ErrorMessage == "الاسم مطلوب");
        Assert.Contains(editor.GetErrors(nameof(editor.UserName)), e => e.ErrorMessage == "اسم المستخدم مطلوب");

        editor.DisplayName = "User";
        editor.UserName = "user";
        Assert.False(editor.SaveCommand.CanExecute(null));
        editor.Password = "secret";
        Assert.False(editor.SaveCommand.CanExecute(null));
        editor.ConfirmPassword = "different";
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.Contains(editor.GetErrors(nameof(editor.ConfirmPassword)), e => e.ErrorMessage == "كلمتا المرور غير متطابقتين.");

        editor.ConfirmPassword = "secret";
        Assert.True(editor.SaveCommand.CanExecute(null));
        editor.IsSaving = true;
        Assert.False(editor.SaveCommand.CanExecute(null));
        editor.IsSaving = false;
        Assert.True(editor.SaveCommand.CanExecute(null));
        editor.DisplayName = " ";
        Assert.False(editor.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void ChangingPasswordRevalidatesConfirmationBeforeCommandNotification()
    {
        var editor = new CreateUserViewModel(Moq.Mock.Of<IRequestExecutor>())
        {
            DisplayName = "User", UserName = "user", Password = "secret", ConfirmPassword = "secret"
        };
        bool? canSaveAtNotification = null;
        editor.SaveCommand.CanExecuteChanged += (_, _) => canSaveAtNotification = editor.SaveCommand.CanExecute(null);

        editor.Password = "changed";
        Assert.False(canSaveAtNotification);
        Assert.Contains(editor.GetErrors(nameof(editor.ConfirmPassword)), e => e.ErrorMessage == "كلمتا المرور غير متطابقتين.");
        editor.Password = "secret";
        Assert.True(canSaveAtNotification);
        Assert.Empty(editor.GetErrors(nameof(editor.ConfirmPassword)));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("one", "two")]
    [InlineData(" ", " ")]
    public void UpdateIgnoresHiddenPasswordValidation(string password, string confirmation)
    {
        var editor = new UpdateUserViewModel
        {
            DisplayName = "User", UserName = "user", Password = password, ConfirmPassword = confirmation
        };
        Assert.True(editor.SaveCommand.CanExecute(null));
        editor.UserName = " ";
        Assert.False(editor.SaveCommand.CanExecute(null));
        editor.UserName = "user";
        Assert.True(editor.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void DetailsIsReadOnlyWithoutSaveOrPasswords()
    {
        Moq.Mock<IRequestExecutor> requestExecuterMock = new Moq.Mock<IRequestExecutor>();
        var editor = new UserDetailsViewModel(requestExecuterMock.Object) { DisplayName = "User", UserName = "user" };
        Assert.True(editor.IsReadOnly);
        Assert.False(editor.ShowPassword);
        Assert.False(editor.ShowSaveButton);
        Assert.False(editor.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void AdminAndSelectAllPreservePermissionRules()
    {
        var editor = new CreateUserViewModel(Moq.Mock.Of<IRequestExecutor>()) { IsActive = false };
        editor.IsAdmin = true;
        Assert.True(editor.IsActive);
        Assert.True(editor.Permissions.All(p => p.CanRead && p.CanWrite));
        editor.IsActive = false;
        Assert.False(editor.IsAdmin);
        editor.SelectAllCommand.Execute(null);
        Assert.True(editor.Permissions.All(p => !p.CanRead && !p.CanWrite));
        editor.SelectAllCommand.Execute(null);
        Assert.True(editor.Permissions.All(p => p.CanRead && p.CanWrite));

        var permission = new UserPermissionItem(PermissionFeature.Documents, "Documents");
        permission.CanWrite = true;
        Assert.True(permission.CanRead);
        permission.CanRead = false;
        Assert.False(permission.CanWrite);
    }
}
