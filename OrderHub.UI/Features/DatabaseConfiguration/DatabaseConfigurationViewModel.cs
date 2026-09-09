using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using OrderHub.Application.Configuration;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.DatabaseConfiguration;
public partial class DatabaseConfigurationViewModel : ObservableValidator
{
    private readonly IDatabaseConfigurationService _databaseConfigurationService;
    private readonly IDatabaseService _databaseService;
    private readonly IApplicationInitializationService _applicationInitializationService;

    public DatabaseConfigurationViewModel(
        IDatabaseConfigurationService databaseConfigurationService,
        IDatabaseService databaseService,
        IApplicationInitializationService applicationInitializationService)
    {
        _databaseConfigurationService = databaseConfigurationService;
        _databaseService = databaseService;
        _applicationInitializationService = applicationInitializationService;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _host;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _database;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private int _port;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _password;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _username;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isBuisy;

    private bool CanExecute => 
        !string.IsNullOrWhiteSpace(Host) &&
        Port > 0 &&
        !string.IsNullOrEmpty(Database) &&
        !string.IsNullOrEmpty(Password) &&
        !string.IsNullOrEmpty(Username);

    private bool CanSave => CanExecute && IsConnected;

    public void Load()
    {
        DatabaseSettings databaseSettings = _databaseConfigurationService.GetSettings();

        if(databaseSettings is not null)
        {
            Host = databaseSettings.Host;
            Port = databaseSettings.Port;
            Username = databaseSettings.Username;
            Password = databaseSettings.Password;
            Database = databaseSettings.Database;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        _databaseConfigurationService.SaveSettings(new Application.Configuration.DatabaseSettings()
        {
            Host = Host,
            Database = Database,
            Port = Port,
            Password = Password,
            Username = Username,
        });

        await _applicationInitializationService.InitializeAsync();
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task TestConnection()
    {
        IsBuisy = true;
        IsConnected = await _databaseService.CanConnectAsync(new Application.Configuration.DatabaseSettings()
        {
            Host = Host,
            Database = Database,
            Port = Port,
            Password = Password,
            Username = Username,
        });

        IsBuisy = false;
    }

}
