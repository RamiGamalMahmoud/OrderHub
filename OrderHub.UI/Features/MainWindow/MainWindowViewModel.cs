using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.MainWindow;

internal partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ISessionManager _sessionManager;
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly IWhatsappService _whatsappService;
    [ObservableProperty]
    private NavigationCommand _navigationCommand;

    public MainWindowViewModel(
        INavigationService navigationService,
        ISessionManager sessionManager,
        IMediator mediator,
        IDialogService dialogService,
        IWhatsappService whatsappService,
        IAppState appState)
    {
        _navigationService = navigationService;
        _sessionManager = sessionManager;
        _mediator = mediator;
        _dialogService = dialogService;
        _whatsappService = whatsappService;
        AppState = appState;
        InitializeNavigationCommands();
    }

    public ObservableCollection<NavigationCommand> NavigationCommands { get; } = [];

    private void InitializeNavigationCommands()
    {
        NavigationCommands.Add(new NavigationCommand("الصفحة الرئيسية", "Home", () => _navigationService.NavigateTo<Features.Home.HomeView>()));
        NavigationCommands.Add(new NavigationCommand("الطلبات", "ListBox", () => _navigationService.NavigateTo<Features.Orders.Index.View>()));
        NavigationCommands.Add(new NavigationCommand("المنتجات", "Factory", () => _navigationService.NavigateTo<Features.Products.Index.View>()));
        NavigationCommands.Add(new NavigationCommand("الأقسام", "Factory", () => _navigationService.NavigateTo<Features.Categories.Index.View>()));
        NavigationCommands.Add(new NavigationCommand("العملاء", "AccountTie", () => _navigationService.NavigateTo<Features.Clients.Index.View>()));
        NavigationCommands.Add(new NavigationCommand("الموردون", "BadgeAccount", () => _navigationService.NavigateTo<Features.Suppliers.Index.View>()));
        NavigationCommands.Add(new NavigationCommand("شركات الشحن", "Ship", () => _navigationService.NavigateTo<Features.ShippingCarriers.Index.View>()));
        NavigationCommands.Add(new NavigationCommand("المناديب", "Moped", () => _navigationService.NavigateTo<Features.Deliverymen.Index.View>()));
        
        IsAuthenticated = 
            _sessionManager.CurrentSession is not null && 
            _sessionManager.CurrentSession.Token is not null && 
            !_sessionManager.CurrentSession.Token.IsExpired;
#if DEBUG
        IsAppInDebug = true;
#endif
    }

    partial void OnNavigationCommandChanged(NavigationCommand value)
    {
        value?.Action?.Invoke();
    }

    [RelayCommand]
    private async Task LoginSalla()
    {
        IsAuthenticated = await _mediator.Send(new Application.Commands.AuthCommand());
        if(!IsAuthenticated)
        {
            _dialogService.ShowDialog<Settings.ClientCredentialsSettings.ClientCredentialsSettingsView>();
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginSallaCommand))]
    private bool _isAuthenticated = false;

    [RelayCommand]
    private async Task OpenWhatapp()
    {
        await _whatsappService.StartWhatsApp();
    }

    public bool IsAppInDebug { get; private set; } = false;

    public INavigationService NavigationService => _navigationService;

    public IAppState AppState { get; }
}
