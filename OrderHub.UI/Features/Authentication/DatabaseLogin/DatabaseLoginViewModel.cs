using OrderHub.Application.Context;
using OrderHub.Application.Features.Authentication.DatabaseLogin;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Authentication.DatabaseLogin;
public class DatabaseLoginViewModel : LoginViewModel
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IApplicationInitializationService _applicationInitializationService;

    public DatabaseLoginViewModel(
        IRequestExecutor requestExecutor,
        IApplicationContext applicationContext,
        IApplicationInitializationService applicationInitializationService) : base(applicationContext)
    {
        _requestExecutor = requestExecutor;
        _applicationInitializationService = applicationInitializationService;
    }

    protected override async Task Login()
    {
        Result<UserSession> result = await _requestExecutor
            .ExecuteAsync(new DatabaseLoginCommand(
                Username,
                Password,
                RememberMe));

        if (result.IsSuccess)
        {
            await _applicationInitializationService.EnterApplicationAsync();
        }
        else
            NotificationService.Instance.ShowError(result.ErrorMessage);
    }
}
