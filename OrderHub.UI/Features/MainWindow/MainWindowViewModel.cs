using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Authentication.Logout;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.MainWindow;

internal partial class MainWindowViewModel : ObservableObject
{
    private readonly ISessionManager _sessionManager;
    private readonly IMediator _mediator;
    private readonly IApplicationDirectoriesService _applicationDirectoriesService;
    private readonly IWhatsappService _whatsappService;
    private readonly IMessageService _messageService;
    private readonly IApplicationInitializationService _applicationInitializationService;

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
        IApplicationInitializationService applicationInitializationService)
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
