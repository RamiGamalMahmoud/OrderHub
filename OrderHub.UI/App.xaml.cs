using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.UI.StartUpSteps;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OrderHub.UI;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddServices();
            })
            .Build();

        RegisterGlobalExceptionHandlers();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShowSplashScreen();
        _ = StartAsync();
        //_ = InitializeAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            await Dispatcher.Yield(DispatcherPriority.Render);

            IStartupPipeline startupPipeline = GetService<IStartupPipeline>();

            await startupPipeline.RunAsync();

            ShowMainWindow();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "Application startup");
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            PrepareDirectoriesAsync();

            var dbReady = await EnsureDatabaseAsync();
            if (!dbReady)
                return;

            await InitializeInfrastructureAsync();

#if DEBUG
            await HandleAuthenticationAsync();
#endif

            ShowMainWindow();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "Legacy initialization");
        }
    }

    #region Initialization مراحل التشغيل

    private void PrepareDirectoriesAsync()
    {
        var dirs = GetService<IApplicationDirectoriesService>();

        dirs.EnsureAppDirectoryCreated();
        dirs.EnsureStorageDirectoryCreated();
        dirs.EnsureDatabaseFileCreated();
        dirs.EnsureWhatsAppProfilesDirectoryCreated();
    }

    private async Task<bool> EnsureDatabaseAsync()
    {
        IDatabaseService db = GetService<IDatabaseService>();

        if (!await db.CanConnectAsync())
        {
            await NotifyErrorAsync("خطأ في الاتصال بقاعدة البيانات");
            return false;
        }

        if (await db.HasPendingMigrationsAsync())
        {
            await db.MigrateAsync();
            await NotifySuccessAsync("تم تحديث قاعدة البيانات");
        }

        return true;
    }

    private async Task InitializeInfrastructureAsync()
    {
        await GetService<IWhatsappService>().StartWhatsAppAsync();

        // تقدر تضيف هنا:
        // MQTT, WebSocket, Sync Service ...
    }

    private async Task HandleAuthenticationAsync()
    {
        if (!await HasSavedClientCredentialsAsync())
        {
            ShowClientCredentialsWindow();
            return;
        }

        if (!await HasSavedTokenAsync())
        {
            await SaveTokenAsync();
        }

        await StartNewSessionAsync();
    }

    #endregion

    #region Authentication

    private async Task StartNewSessionAsync()
    {
        ISessionManager sessionManager = GetService<ISessionManager>();
        await sessionManager.StartNewSession();
    }

    private async Task<bool> HasSavedTokenAsync()
    {
        ITokenStorageService tokenStorage = GetService<ITokenStorageService>();
        Token token = await tokenStorage.GetTokenAsync();
        return token != null;
    }

    private async Task SaveTokenAsync()
    {
        IAuthService auth = GetService<IAuthService>();
        ICredentialsService credentialsService = GetService<ICredentialsService>();

        ClientCredentials credentials = await credentialsService.GetClilentCredentialsAsync();

        Result<Token> result = await auth.AuthorizeAsync(credentials);

        if (!result.IsSuccess)
        {
            await NotifyErrorAsync(result.ErrorMessage);
            return;
        }

        ITokenStorageService tokenStorage = GetService<ITokenStorageService>();
        await tokenStorage.SaveTokenAsync(result.Value);
    }

    private async Task<bool> HasSavedClientCredentialsAsync()
    {
        ICredentialsService credentialsService = GetService<ICredentialsService>();
        ClientCredentials credentials = await credentialsService.GetClilentCredentialsAsync();
        return credentials != null;
    }

    #endregion

    #region UI Navigation

    private void ShowSplashScreen()
    {
        MainWindow = GetService<Features.Splash>();
        MainWindow.Show();
    }

    private void ShowClientCredentialsWindow()
    {
        SwitchWindow<Features.Settings.ClientCredentialsSettings.ClientCredentialsSettingsView>();
    }

    private void ShowMainWindow()
    {
        SwitchWindow<Features.MainWindow.MainWindowView>();
    }

    private void SwitchWindow<T>() where T : Window
    {
        Window oldWindow = MainWindow;

        T newWindow = GetService<T>();
        MainWindow = newWindow;

        newWindow.Show();
        oldWindow?.Close();
    }

    #endregion

    #region Exception Handling

    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        _ = HandleExceptionAsync(ex ?? new Exception("Unknown error"), "AppDomain.CurrentDomain.UnhandledException");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
#if !DEBUG
        e.Handled = true;
#endif
        _ = HandleExceptionAsync(e.Exception, "DispatcherUnhandledException");
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        _ = HandleExceptionAsync(e.Exception, "TaskScheduler.UnobservedTaskException");
    }

    #endregion

    #region Notifications

    private async Task NotifyErrorAsync(string message)
    {
        await GetService<IMediator>().Publish(new Application.Notifications.ErrorNotification(message));
    }

    private async Task NotifySuccessAsync(string message)
    {
        await GetService<IMediator>().Publish(new Application.Notifications.SuccessNotification(message));
    }

    #endregion

    #region Helpers

    private T GetService<T>() => _host.Services.GetRequiredService<T>();

    private async Task HandleExceptionAsync(Exception exception, string context)
    {
        try
        {
            IAppLogger logger = GetService<IAppLogger>();
            await logger.LogErrorAsync($"Unhandled exception in {context}.", exception);
        }
        catch
        {
        }

#if DEBUG
        string message = exception.Message;
#else
        string message = "حدث خطأ غير متوقع. تم حفظ تفاصيل الخطأ في ملف السجل.";
#endif

        await NotifyErrorAsync(message);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        await GetService<IMessageService>().StopAsync();
        GetService<IWhatsappService>().Close();
        await _host.StopAsync();
    }

    #endregion
}
