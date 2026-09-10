using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Authentication.Logout;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace OrderHub.UI.Features.MainWindow;

internal partial class MainWindowViewModel : ObservableObject
{
    private CancellationTokenSource _updatingCTS;
    private readonly ISessionManager _sessionManager;
    private readonly IMediator _mediator;
    private readonly IApplicationDirectoriesService _applicationDirectoriesService;
    private readonly IWhatsappService _whatsappService;
    private readonly IMessageService _messageService;
    private readonly IApplicationInitializationService _applicationInitializationService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private NavigationCommand _selectedNavigationCommand;

    public MainWindowViewModel(
        ISessionManager sessionManager,
        IMediator mediator,
        IWhatsappService whatsappService,
        IMessageService messageService,
        IAppState appState,
        IApplicationDirectoriesService applicationDirectoriesService,
        IApplicationContext applicationContext,
        IApplicationInitializationService applicationInitializationService,
        IUpdateService updateService)
    {
        _sessionManager = sessionManager;
        _mediator = mediator;
        _whatsappService = whatsappService;
        _messageService = messageService;
        _applicationDirectoriesService = applicationDirectoriesService;
        AppState = appState;
        InitializeNavigationCommands();
        IsAuthenticated = !_sessionManager.CurrentSession?.Token?.IsExpired ?? false;
        ApplicationContext = applicationContext;
        _applicationInitializationService = applicationInitializationService;
        _updateService = updateService;
    }

    public ObservableCollection<NavigationCommand> NavigationCommands { get; } = [];

    public string Title => $"OrderHub - {ApplicationContext.Application.InstallationType} - v{ApplicationContext.Application.Version}";
    private void InitializeNavigationCommands()
    {
        NavigationCommands.Add(new NavigationCommand(
            "لوحة التحكم",
            "ViewDashboard",
            () => NavigationService.Instance.NavigateTo<Features.Dashboard.DashboardView>()));

        NavigationCommands.Add(new NavigationCommand(
            "الطلبات",
            "ListBox",
            () => NavigationService.Instance.NavigateTo<Features.Orders.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "المنتجات",
            "Factory",
            () => NavigationService.Instance.NavigateTo<Features.Products.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "الأقسام",
            "Factory",
            () => NavigationService.Instance.NavigateTo<Features.Categories.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "المدن",
            "MapMarker",
            () => NavigationService.Instance.NavigateTo<Features.Cities.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "العملاء",
            "AccountTie",
            () => NavigationService.Instance.NavigateTo<Features.Clients.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "الموردون",
            "BadgeAccount",
            () => NavigationService.Instance.NavigateTo<Features.Suppliers.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "شركات الشحن",
            "Ship",
            () => NavigationService.Instance.NavigateTo<Features.ShippingCarriers.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "المناديب",
            "Moped",
            () => NavigationService.Instance.NavigateTo<Features.Deliverymen.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "مجموعات الواتساب",
            "Whatsapp",
            () => NavigationService.Instance.NavigateTo<Features.WhatsappGroups.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "سجل الرسائل",
            "MessageMinusOutline",
            () => NavigationService.Instance.NavigateTo<Features.Messages.Index.View>()));

        NavigationCommands.Add(new NavigationCommand(
            "الإعدادات",
            "CogOutline",
            () => NavigationService.Instance.NavigateTo<Features.Settings.Home.SettingsHomeView>()));
    }

    [RelayCommand]
    private async Task LoginSalla()
    {
        IsAuthenticated = await _mediator.Send(new Application.Commands.AuthCommand());
        if (!IsAuthenticated)
        {
            await DialogService.Instance.ShowDialog<Settings.ClientCredentialsSettings.ClientCredentialsSettingsView>("إعدادات الاتصال بسلة");
        }
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = _applicationDirectoriesService.DataDirectory,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginSallaCommand))]
    private bool _isAuthenticated = false;

    [RelayCommand]
    private async Task OpenWhatapp()
    {
        bool started = await _whatsappService.StartWhatsAppAsync();
        if (!started)
        {
            NotificationService.Instance.ShowError("تعذر تشغيل واتساب ويب.");
            return;
        }

        await _messageService.StartAsync();
        NotificationService.Instance.ShowSuccess("تم تشغيل واتساب ويب وخدمة الرسائل.");
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _mediator.Send(new LogoutCommand());
        await _applicationInitializationService.InitializeAsync();
    }

    partial void OnSelectedNavigationCommandChanged(NavigationCommand value)
    {
        value?.Action?.Invoke();
    }

    public bool CanUpdate =>
        ApplicationContext.Update is not null &&
        ApplicationContext.Update.NeedToUpdate;

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private async Task Update()
    {
        bool update = await DialogService.Instance.ConfirmAsync(
            """
            يتوفر إصدار جديد من البرنامج. هل تريد تنزيل التحديث وتثبيته الآن؟
            """);
        if (!update)
            return;
        _updatingCTS = new CancellationTokenSource();
        IsUpdating = true;

        var progress = new Progress<double>(value =>
        {
            UpdateProgress = value;
        });



        try
        {
            string installPath = await _updateService.DownloadUpdateAsync(ApplicationContext.Update, progress, _updatingCTS.Token);
            _updateService.RunInstaller(installPath);
        }
        catch (OperationCanceledException)
        {
            UpdateProgress = 0;
        }
        catch (Exception)
        {
            // Actual download error
        }

        IsUpdating = false;
    }

    [RelayCommand]
    private async Task CancelUpdate()
    {
        if (!IsUpdating || _updatingCTS is null)
            return;

        bool cancelUpdate = await DialogService.Instance.ConfirmAsync(
            """
            هل أنت متأكد من إلغاء التحديث؟ سيتم إيقاف عملية التنزيل الحالية ولن يتم تثبيت الإصدار الجديد.
            """);

        if (!cancelUpdate)
            return;

        await _updatingCTS.CancelAsync();


    }

    [ObservableProperty]
    private bool _isUpdating;

    private double _updateProgress;

    public double UpdateProgress
    {
        get => _updateProgress;
        set => SetProperty(ref _updateProgress, value);
    }

    [ObservableProperty]
    private string _message;

    public bool IsAppInDebug =>
#if DEBUG
    true;
#else
    false;
#endif

    public NavigationService NavigationService => NavigationService.Instance;
    public IAppState AppState { get; }
    public IApplicationContext ApplicationContext { get; }
}
