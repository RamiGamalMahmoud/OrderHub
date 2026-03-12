using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
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

        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        System.Windows.Application.Current.DispatcherUnhandledException += CurrentDispatcherUnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _host.Services.GetRequiredService<IMediator>().Publish(new Application.Notifications.ErrorNotification(e.Exception.Message));
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _host.Services.GetRequiredService<IMediator>().Publish(new Application.Notifications.ErrorNotification(e.Exception.Message));
    }

    private void ShowSplashScreen()
    {
        MainWindow = _host.Services.GetRequiredService<Features.Splash>();
        MainWindow.Show();
    }

    private void CurrentDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
#if !DEBUG
        e.Handled = true;
#endif 
    }

    private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        IApplicationDirectoriesService applicationDirectoriesService = GetService<IApplicationDirectoriesService>();
        applicationDirectoriesService.EnsureAppDirectoryCreated();
        applicationDirectoriesService.EnsureStorageDirectoryCreated();
        applicationDirectoriesService.EnsureDatabaseFileCreated();
        applicationDirectoriesService.EnsureWhatAppProfilesDirectoryCreated();

        IDatabaseService databaseService = GetService<IDatabaseService>();
        bool canConnect = await databaseService.CanConnectAsync();

        ShowSplashScreen();

        if (!canConnect)
        {
            await GetService<IMediator>().Publish(new Application.Notifications.ErrorNotification("خطاء في الاتصال بقاعدة البيانات"));
            return;
        }
        if (await databaseService.HasPendingMigrationsAsync())
        {
            await databaseService.MigrateAsync();
            await GetService<IMediator>().Publish(new Application.Notifications.SuccessNotification("تم تحديث قاعدة البيانات."));
        }
#if DEBUG

        if (!await HasSaveClientCredentials())
        {
            ShowClientCredentialsWindow();
        }

        else
        {
            if (!await HasSaveToken())
            {
                await SaveToken();
            }
            await StartNewSession();
        }
#endif

        ShowMainWindow();

        await _host.RunAsync();
    }

    private async Task StartNewSession()
    {
        ISessionManager sessionManager = GetService<ISessionManager>();
        await sessionManager.StartNewSession();
    }

    private async Task<bool> HasSaveToken()
    {
        ITokenStorageService tokenStorageService = GetService<ITokenStorageService>();
        Token token = await tokenStorageService.GetTokenAsync();
        return token is not null;
    }

    private async Task SaveToken()
    {
        IAuthService authService = GetService<IAuthService>();

        ICredentialsService credentialsService = GetService<ICredentialsService>();

        ClientCredentials clientCredentials = await credentialsService.GetClilentCredentialsAsync();

        Result<Token> tokenResult = await authService.AuthorizeAsync(clientCredentials);

        ITokenStorageService tokenStorageService = GetService<ITokenStorageService>();

        if (tokenResult.IsSuccess)
        {
            await tokenStorageService.SaveTokenAsync(tokenResult.Value);
        }
    }

    private async Task<bool> HasSaveClientCredentials()
    {
        ICredentialsService credentialsService = GetService<ICredentialsService>();
        ClientCredentials clientCredentials = await credentialsService.GetClilentCredentialsAsync();
        return clientCredentials is not null;
    }

    private T GetService<T>()
    {
        return _host.Services.GetRequiredService<T>();
    }

    private void ShowClientCredentialsWindow()
    {
        Window window = MainWindow;
        MainWindow = GetService<Features.Settings.ClientCredentialsSettings.ClientCredentialsSettingsView>();
        MainWindow.Show();
        window.Close();
    }

    private void ShowMainWindow()
    {
        Window window = MainWindow;
        MainWindow = _host.Services.GetRequiredService<Features.MainWindow.MainWindowView>();
        MainWindow.Show();
        window.Close();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        await _host.StopAsync();
    }
}
