using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Features.Authentication.Logout;
using OrderHub.Application.Interfaces;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class AuthCommands : ObservableObject, ICommandGroup
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IApplicationInitializationService _applicationInitializationService;

    public AuthCommands(
        IApplicationInitializationService applicationInitializationService,
        IRequestExecutor requestExecutor)
    {
        _applicationInitializationService = applicationInitializationService;
        _requestExecutor = requestExecutor;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await DialogService.Instance.ConfirmAsync("أنت على وشك تسجيل خرو, هل تريد الاستمرا ؟"))
            return;

        await _requestExecutor.ExecuteAsync(new LogoutCommand());

        NavigationService.Instance.Reset();

        await _applicationInitializationService.InitializeAsync();
    }
}