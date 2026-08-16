using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using OrderHub.UI.StartUpSteps;
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
                services.AddServices();
            })
            .Build();

        RegisterGlobalExceptionHandlers();
        DialogService.Init(_host.Services);
        NavigationService.Init(_host.Services);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        CultureInfo culture = new CultureInfo("en-US");

        culture.DateTimeFormat.Calendar = new GregorianCalendar();
        culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Sunday;
        culture.NumberFormat.DigitSubstitution = DigitShapes.None;

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
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

    private void ShowSplashScreen()
    {
        MainWindow = GetService<Features.Splash>();
        MainWindow.Show();
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
