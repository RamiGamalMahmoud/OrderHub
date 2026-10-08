using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Settings.ClientCredentialsSettings;

public partial class ClientCredentialsSettingsViewModel : ObservableValidator
{
    private readonly ICredentialsService _credentialsService;

    [ObservableProperty]
    [Required(ErrorMessage = "معرف العميل مطلوب")]
    [NotifyCanExecuteChangedFor(nameof(SaveCredentialsCommand))]
    [NotifyDataErrorInfo]
    private string _clientId;

    [ObservableProperty]
    [Required(ErrorMessage = "المفتاح السري للعميل مطلوب")]
    [NotifyCanExecuteChangedFor(nameof(SaveCredentialsCommand))]
    [NotifyDataErrorInfo]
    private string _clientSecret;

    public ClientCredentialsSettingsViewModel(ICredentialsService credentialsService)
    {
        _credentialsService = credentialsService;
    }

    [RelayCommand]
    private async Task LoadCredentialsAsync()
    {
        ClientCredentials clientCredentials = await _credentialsService.GetClilentCredentialsAsync();
        ClientId = clientCredentials?.ClientId;
        ClientSecret = clientCredentials?.ClientSecret;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveCredentialsAsync()
    {
        ClientCredentials clientCredentials = new ClientCredentials(ClientId, ClientSecret);
        await _credentialsService.SaveClientCredentialsAsync(clientCredentials);
    }

    private bool CanSave() => !HasErrors && !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);
}
