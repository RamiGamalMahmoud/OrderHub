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

    public ApplicationInitializationService(
        IApplicationContext applicationContext,
        IConnectionConfigurationStorage connectionConfigurationStorage,
        IDatabaseService databaseService,
        IUserSessionStorage userSessionStorage,
        IUserSessionValidator userSessionValidator,
        IAppLogger appLogger)
    {
        _applicationContext = applicationContext;
        _connectionConfigurationStorage = connectionConfigurationStorage;
        _databaseService = databaseService;
        _userSessionStorage = userSessionStorage;
        _userSessionValidator = userSessionValidator;
        _appLogger = appLogger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _appLogger.LogInfoAsync("بدء تهيئة التطبيق.");

            NotificationService.Instance.Show("بدء تهيئة التطبيق...");

            InstallationType installationType =
                _applicationContext.Application.InstallationType;

            await _appLogger.LogInfoAsync(
                $"نوع تثبيت التطبيق: {installationType}.");

            NotificationService.Instance.Show(
                $"نوع التثبيت: {installationType}");

            Func<Task<bool>> initializationAction = installationType switch
            {
                InstallationType.Server => ServerInitializationAsync,
                InstallationType.Client => ClientInitializationAsync,
                _ => throw new InvalidOperationException(
                    "Invalid application installation type.")
            };

            bool connectionReady = await initializationAction();

            if (!connectionReady)
            {
                await _appLogger.LogInfoAsync(
                    "لم تكتمل تهيئة الاتصال، وتم فتح شاشة الإعداد المطلوبة.");

                NotificationService.Instance.Show(
                    "تم إيقاف التهيئة مؤقتًا، في انتظار إكمال إعداد الاتصال.");

                return;
            }

            await _appLogger.LogInfoAsync(
                "تم تجهيز الاتصال بقاعدة البيانات بنجاح.");

            NotificationService.Instance.ShowSuccess(
                "تم تجهيز الاتصال بقاعدة البيانات بنجاح.");

            bool enteredApplication = await EnterApplicationAsync();

            if (!enteredApplication)
            {
                await _appLogger.LogInfoAsync(
                    "لم يتم العثور على جلسة مستخدم صالحة.");

                NotificationService.Instance.Show(
                    "لا توجد جلسة دخول صالحة، سيتم فتح شاشة تسجيل الدخول.");

                DialogService.Instance.SwitchWindow<
                    Features.Authentication.DatabaseLogin.DatabaseLoginView>();

                return;
            }

            await _appLogger.LogInfoAsync(
                "تمت تهيئة التطبيق والدخول إليه بنجاح.");

            NotificationService.Instance.ShowSuccess(
                "تم تسجيل الدخول والدخول إلى التطبيق بنجاح.");
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
        await _appLogger.LogInfoAsync(
            "بدء تهيئة اتصال السيرفر.");

        NotificationService.Instance.Show(
            "جاري تحميل إعدادات اتصال قاعدة البيانات...");

        ConnectionConfiguration connectionConfiguration =
            await _connectionConfigurationStorage.LoadAsync();

        if (connectionConfiguration is null)
        {
            await _appLogger.LogInfoAsync(
                "لم يتم العثور على إعدادات اتصال قاعدة البيانات على السيرفر.");

            NotificationService.Instance.ShowError(
                "لم يتم العثور على إعدادات اتصال قاعدة البيانات.");

            NotificationService.Instance.Show(
                "سيتم فتح شاشة إعداد اتصال قاعدة البيانات.");

            DialogService.Instance.SwitchWindow<
                Features.DatabaseConfiguration.DatabaseConfigurationView>();

            return false;
        }

        await _appLogger.LogInfoAsync(
            "تم تحميل إعدادات اتصال قاعدة البيانات على السيرفر.");

        NotificationService.Instance.ShowSuccess(
            "تم تحميل إعدادات اتصال قاعدة البيانات.");

        NotificationService.Instance.Show(
            "جاري اختبار الاتصال بقاعدة البيانات...");

        bool canConnect = await _databaseService.CanConnectAsync();

        if (!canConnect)
        {
            await _appLogger.LogErrorAsync(
                "فشل الاتصال بقاعدة البيانات على السيرفر.");

            NotificationService.Instance.ShowError(
                "تعذر الاتصال بقاعدة البيانات.");

            NotificationService.Instance.Show(
                "سيتم فتح شاشة إعداد اتصال قاعدة البيانات.");

            DialogService.Instance.SwitchWindow<
                Features.DatabaseConfiguration.DatabaseConfigurationView>();

            return false;
        }

        await _appLogger.LogInfoAsync(
            "تم الاتصال بقاعدة البيانات بنجاح على السيرفر.");

        NotificationService.Instance.ShowSuccess(
            "تم الاتصال بقاعدة البيانات بنجاح.");

        return true;
    }

    private async Task<bool> ClientInitializationAsync()
    {
        await _appLogger.LogInfoAsync(
            "بدء تهيئة اتصال العميل.");

        NotificationService.Instance.Show(
            "جاري تحميل إعدادات اتصال قاعدة البيانات...");

        ConnectionConfiguration connectionConfiguration =
            await _connectionConfigurationStorage.LoadAsync();

        if (connectionConfiguration is null)
        {
            await _appLogger.LogInfoAsync(
                "لم يتم العثور على إعدادات اتصال قاعدة البيانات على العميل.");

            NotificationService.Instance.Show(
                "لم يتم العثور على إعدادات اتصال قاعدة البيانات.");

            NotificationService.Instance.Show(
                "سيتم فتح تسجيل الدخول إلى السيرفر للحصول على الإعدادات.");

            DialogService.Instance.SwitchWindow<
                Features.Authentication.ApiLogin.ApiLoginView>();

            return false;
        }

        await _appLogger.LogInfoAsync(
            "تم تحميل إعدادات اتصال قاعدة البيانات على العميل.");

        NotificationService.Instance.ShowSuccess(
            "تم تحميل إعدادات اتصال قاعدة البيانات.");

        NotificationService.Instance.Show(
            "جاري اختبار الاتصال بقاعدة البيانات...");

        bool canConnect = await _databaseService.CanConnectAsync();

        if (!canConnect)
        {
            await _appLogger.LogErrorAsync(
                "فشل الاتصال بقاعدة البيانات على العميل.");

            NotificationService.Instance.ShowError(
                "تعذر الاتصال بقاعدة البيانات.");

            NotificationService.Instance.Show(
                "سيتم فتح تسجيل الدخول إلى السيرفر للحصول على إعدادات اتصال جديدة.");

            DialogService.Instance.SwitchWindow<
                Features.Authentication.ApiLogin.ApiLoginView>();

            return false;
        }

        await _appLogger.LogInfoAsync(
            "تم الاتصال بقاعدة البيانات بنجاح على العميل.");

        NotificationService.Instance.ShowSuccess(
            "تم الاتصال بقاعدة البيانات بنجاح.");

        return true;
    }

    public async Task<bool> EnterApplicationAsync()
    {
        await _appLogger.LogInfoAsync(
            "بدء التحقق من جلسة المستخدم.");

        NotificationService.Instance.Show(
            "جاري التحقق من جلسة المستخدم...");

        UserSession userSession =
            _applicationContext.User ?? _userSessionStorage.Load();

        if (userSession is null)
        {
            await _appLogger.LogInfoAsync(
                "لم يتم العثور على جلسة مستخدم محفوظة.");

            NotificationService.Instance.Show(
                "لم يتم العثور على جلسة مستخدم محفوظة.");

            return false;
        }

        await _appLogger.LogInfoAsync(
            $"جاري التحقق من جلسة المستخدم: {userSession.UserName}");

        NotificationService.Instance.Show(
            $"جاري التحقق من المستخدم: {userSession.UserName}...");

        bool isValid =
            await _userSessionValidator.IsValidAsync(userSession);

        if (!isValid)
        {
            await _appLogger.LogInfoAsync(
                $"جلسة المستخدم غير صالحة أو الحساب غير نشط: {userSession.UserName}");

            NotificationService.Instance.ShowError(
                "جلسة المستخدم غير صالحة أو أن الحساب لم يعد نشطًا.");

            await DialogService.Instance.ConfirmAsync(
                "حسابك لم يعد نشطًا، وسيتم تسجيل خروجك من النظام.");

            _userSessionStorage.Clear();
            _applicationContext.ClearUser();

            await _appLogger.LogInfoAsync(
                $"تم حذف الجلسة المحفوظة للمستخدم: {userSession.UserName}");

            NotificationService.Instance.Show(
                "تم تسجيل الخروج وحذف الجلسة المحفوظة.");

            return false;
        }

        _applicationContext.SetUser(userSession);

        await _appLogger.LogInfoAsync(
            $"تم التحقق من جلسة المستخدم بنجاح: {userSession.UserName}");

        NotificationService.Instance.ShowSuccess(
            $"تم التحقق من المستخدم {userSession.UserName} بنجاح.");

        NotificationService.Instance.Show(
            "جاري فتح التطبيق...");

        DialogService.Instance.SwitchWindow<
            Features.MainWindow.MainWindowView>();

        await _appLogger.LogInfoAsync(
            "تم فتح التطبيق بنجاح.");

        return true;
    }
}
