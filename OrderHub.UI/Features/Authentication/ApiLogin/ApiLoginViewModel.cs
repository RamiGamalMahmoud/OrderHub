using OrderHub.Application.Context;
using OrderHub.Application.Features.Authentication.ApiLogin;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Authentication.ApiLogin;
public class ApiLoginViewModel : LoginViewModel
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IApplicationInitializationService _applicationInitializationService;

    public ApiLoginViewModel(
        IRequestExecutor requestExecutor,
        IApplicationContext applicationContext,
        IApplicationInitializationService applicationInitializationService) : base(applicationContext)
    {
        _requestExecutor = requestExecutor;
        _applicationInitializationService = applicationInitializationService;
    }

    protected override async Task Login()
    {
        Result<AuthResponse> result = await _requestExecutor
            .ExecuteAsync(new ApiLoginCommand(
                Username, 
                Password, 
                RememberMe));

        if (result.IsSuccess)
        {
            await _applicationInitializationService.InitializeAsync();
        }
    }
}
