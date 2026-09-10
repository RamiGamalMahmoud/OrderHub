using OrderHub.Application.Context;
using OrderHub.Application.Infrastructure.Connection;
using OrderHub.Application.Infrastructure.Installation;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.Services;

public sealed class ApplicationInitializationService : IApplicationInitializationService
{
    private readonly IApplicationContext _applicationContext;
    private readonly IConnectionConfigurationStorage _connectionConfigurationStorage;
    private readonly IDatabaseService _databaseService;
    private readonly IUserSessionStorage _userSessionStorage;
    private readonly IUserSessionValidator _userSessionValidator;
    private readonly IAppLogger _appLogger;
    private readonly IInitialAdminService _initialAdminService;

    public ApplicationInitializationService(
        IApplicationContext applicationContext,
        IConnectionConfigurationStorage connectionConfigurationStorage,
        IDatabaseService databaseService,
        IUserSessionStorage userSessionStorage,
        IUserSessionValidator userSessionValidator,
        IAppLogger appLogger,
        IInitialAdminService initialAdminService)
    {
        _applicationContext = applicationContext;
        _connectionConfigurationStorage = connectionConfigurationStorage;
        _databaseService = databaseService;
        _userSessionStorage = userSessionStorage;
        _userSessionValidator = userSessionValidator;
        _appLogger = appLogger;
        _initialAdminService = initialAdminService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            InstallationType installationType =
                _applicationContext.Application.InstallationType;

            Func<Task<bool>> initializationAction = installationType switch
            {
                InstallationType.Server => ServerInitializationAsync,
                InstallationType.Client => ClientInitializationAsync,
                _ => throw new InvalidOperationException(
                    "Invalid application installation type.")
            };

            bool connectionReady = await initializationAction();

            if (!connectionReady)
                return;

            bool enteredApplication = await EnterApplicationAsync();

            if (!enteredApplication)
            {
                DialogService.Instance.SwitchWindow<
                    Features.Authentication.DatabaseLogin.DatabaseLoginView>();

                return;
            }
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync(
                "حدث خطأ أثناء تهيئة التطبيق.",
                ex);

            NotificationService.Instance.ShowError(
                "حدث خطأ أثناء تهيئة التطبيق.");
        }
    }

    private async Task<bool> ServerInitializationAsync()
    {
        ConnectionConfiguration connectionConfiguration =
            await _connectionConfigurationStorage.LoadAsync();

        if (connectionConfiguration is null)
        {
            DialogService.Instance.SwitchWindow<
                Features.DatabaseConfiguration.DatabaseConfigurationView>();

            return false;
        }

        bool canConnect = await _databaseService.CanConnectAsync();

        if (!canConnect)
        {
            await _appLogger.LogErrorAsync(
                "فشل الاتصال بقاعدة البيانات على السيرفر.");

            NotificationService.Instance.ShowError(
                "تعذر الاتصال بقاعدة البيانات.");

            DialogService.Instance.SwitchWindow<
                Features.DatabaseConfiguration.DatabaseConfigurationView>();

            return false;
        }

        if (!await _initialAdminService.HasAdminAsync())
        {
            DialogService.Instance.SwitchWindow<Features.Administration.Admin.CreateAdmin.CreateAdminView>();

            return false;
        }

        return true;
    }

    private async Task<bool> ClientInitializationAsync()
    {
        ConnectionConfiguration connectionConfiguration =
            await _connectionConfigurationStorage.LoadAsync();

        if (connectionConfiguration is null)
        {
            DialogService.Instance.SwitchWindow<
                Features.Authentication.ApiLogin.ApiLoginView>();

            return false;
        }

        bool canConnect = await _databaseService.CanConnectAsync();

        if (!canConnect)
        {
            await _appLogger.LogErrorAsync(
                "فشل الاتصال بقاعدة البيانات على العميل.");

            NotificationService.Instance.ShowError(
                "تعذر الاتصال بقاعدة البيانات.");

            DialogService.Instance.SwitchWindow<
                Features.Authentication.ApiLogin.ApiLoginView>();

            return false;
        }

        return true;
    }

    public async Task<bool> EnterApplicationAsync()
    {
        UserSession userSession =
            _applicationContext.User ?? _userSessionStorage.Load();

        if (userSession is null)
            return false;

        bool isValid =
            await _userSessionValidator.IsValidAsync(userSession);

        if (!isValid)
        {
            await _appLogger.LogErrorAsync(
                "جلسة المستخدم غير صالحة أو أن الحساب غير نشط.");

            NotificationService.Instance.ShowError(
                "جلسة المستخدم غير صالحة أو أن الحساب لم يعد نشطًا.");

            await DialogService.Instance.ConfirmAsync(
                "حسابك لم يعد نشطًا، وسيتم تسجيل خروجك من النظام.");

            _userSessionStorage.Clear();
            _applicationContext.ClearUser();

            return false;
        }

        _applicationContext.SetUser(userSession);

        DialogService.Instance.SwitchWindow<
            Features.MainWindow.MainWindowView>();

        return true;
    }
}
