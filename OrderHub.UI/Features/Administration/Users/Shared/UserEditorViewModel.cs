using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using OrderHub.UI.Features.Administration.Users.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

public abstract partial class UserEditorViewModel : ObservableValidator
{
    protected UserEditorViewModel()
    {
        ValidateAllProperties();

        Permissions.Clear();

        var permissionFeatures = EnumItems.For<PermissionFeature>();

        foreach(var permissionFeature in permissionFeatures)
        {
            Permissions.Add(new UserPermissionItem(
                permissionFeature.Value,
                permissionFeature.DisplayName));
        }
    }

    public ObservableCollection<UserPermissionItem> Permissions { get; } = [];

    [ObservableProperty]
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _userName;

    [ObservableProperty]
    [Required(ErrorMessage = "الاسم مطلوب")]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _displayName;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(PasswordConfirmed))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _password;

    [ObservableProperty]
    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
    [property: Compare(
        nameof(Password),
        ErrorMessage = "كلمتا المرور غير متطابقتين.")]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(PasswordConfirmed))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _confirmPassword;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAllSelected;

    // Editor state

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isReadOnly = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _showSaveButton;

    [ObservableProperty]
    private string _saveButtonText = string.Empty;

    public abstract string Title { get; }

    public abstract bool ShowPassword { get; }

    public bool PasswordConfirmed =>
        !string.IsNullOrEmpty(Password) &&
        !string.IsNullOrEmpty(ConfirmPassword) &&
        Password == ConfirmPassword;

    public bool CanSave =>
        ShowSaveButton &&
        !GetErrors(nameof(DisplayName)).Any() &&
        !GetErrors(nameof(UserName)).Any() &&
        (!ShowPassword || (!HasErrors && PasswordConfirmed)) &&
        !IsSaving;

    partial void OnPasswordChanged(string value)
    {
        ValidateProperty(
            ConfirmPassword,
            nameof(ConfirmPassword));
    }

    partial void OnIsActiveChanged(bool value)
    {
        if (!value)
            IsAdmin = false;
    }

    partial void OnIsAdminChanged(bool value)
    {
        if (!value)
            return;

        IsActive = true;

        foreach (var permission in Permissions)
        {
            permission.CanRead = true;
            permission.CanWrite = true;
        }

        IsAllSelected = true;
    }

    [RelayCommand]
    private void SelectAll()
    {
        IsAllSelected = !IsAllSelected;

        foreach (var permission in Permissions)
        {
            permission.CanRead = IsAllSelected;
            permission.CanWrite = IsAllSelected;
        }
    }

    [RelayCommand]
    public virtual Task LoadAsync()
        => Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanSave))]
    public virtual Task SaveAsync()
        => Task.CompletedTask;

}