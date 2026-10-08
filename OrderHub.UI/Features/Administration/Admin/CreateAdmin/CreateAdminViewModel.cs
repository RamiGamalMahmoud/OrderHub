using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Features.Administration.Admin.CreateAdmin;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.UI.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Administration.Admin.CreateAdmin;
public partial class CreateAdminViewModel : ObservableValidator
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IApplicationInitializationService _applicationInitializationService;

    public CreateAdminViewModel(
        IRequestExecutor requestExecutor,
        IApplicationInitializationService applicationInitializationService)
    {
        _requestExecutor = requestExecutor;
        ValidateAllProperties();
        _applicationInitializationService = applicationInitializationService;
    }

    [ObservableProperty]
    [Required(ErrorMessage = "الاسم مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _displayName;

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _userName;

    [ObservableProperty]
    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    [NotifyPropertyChangedFor(nameof(PasswordConfirmed))]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _password;

    [ObservableProperty]
    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    [NotifyPropertyChangedFor(nameof(PasswordConfirmed))]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _confirmPassword;

    public bool PasswordConfirmed =>
        !string.IsNullOrEmpty(Password) &&
        !string.IsNullOrEmpty(ConfirmPassword) &&
        Password == ConfirmPassword;

    private bool CanCreate =>
        !HasErrors &&
        PasswordConfirmed;

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task Create()
    {
        Result result = await _requestExecutor.ExecuteAsync(new CreateAdminCommand(
            UserName,
            DisplayName,
            Password));

        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم إنشاء حساب المدير.");
            await _applicationInitializationService.InitializeAsync();
        }
        else
            NotificationService.Instance.ShowError(result.ErrorMessage);
    }
}
