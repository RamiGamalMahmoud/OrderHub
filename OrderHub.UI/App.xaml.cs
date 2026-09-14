using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notification.Wpf;
using OrderHub.Application.Infrastructure.Connection;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Commands;
using OrderHub.UI.DependencyInjection;
using OrderHub.UI.Services;
using OrderHub.UI.StartUpSteps;
using OrderHub.UI.States;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.Threading;
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
                services.AddUI();
            })
            .Build();

        RegisterGlobalExceptionHandlers();
        DialogService.Init(_host.Services);
        NavigationService.Init(_host.Services);
        NotificationService.Initialize(_host.Services.GetRequiredService<INotificationManager>());
        AppCommands.SetInstance(_host.Services.GetRequiredService<IAppCommands>());
        AppState.SetInstance(_host.Services.GetRequiredService<IAppState>());

        RegisterApplicationResources();
    }

    private void RegisterApplicationResources()
    {
        Resources["AppCommands"] =
            _host.Services.GetRequiredService<IAppCommands>();

        Resources["AppState"] =
            _host.Services.GetRequiredService<AppState>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        CultureInfo culture = new CultureInfo("en-US");

        culture.DateTimeFormat.Calendar = new GregorianCalendar();
        culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Sunday;
        culture.NumberFormat.DigitSubstitution = DigitShapes.None;

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        base.OnStartup(e);

        DialogService.Instance.SwitchWindow<Features.Splash>();
        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            await Dispatcher.Yield(DispatcherPriority.Render);

            await RunStartUpPipeline();

            await InitializeApplicationAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "Application startup");
        }
    }

    private async Task RunStartUpPipeline()
    {
        await _host.Services.CreateScope()
            .ServiceProvider
            .GetRequiredService<IStartupPipeline>()
            .RunAsync();

    }


    private async Task InitializeApplicationAsync()
    {
        await GetService<IConnectionConfigurationStorage>()
            .LoadAsync();

        await GetService<IApplicationInitializationService>()
            .InitializeAsync();
    }

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

        NotificationService.Instance.ShowError(message);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        await GetService<IMessageService>().StopAsync();
        GetService<IWhatsappService>().Stop();
        await _host.StopAsync();
    }

    #endregion
}
