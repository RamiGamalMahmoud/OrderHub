using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Context;
using System.Threading.Tasks;

public abstract partial class LoginViewModel : ObservableValidator
{
    public IApplicationContext ApplicationContext { get; }

    protected LoginViewModel(IApplicationContext applicationContext)
    {
        ApplicationContext = applicationContext;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    private bool CanLogin =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    protected abstract Task Login();
}