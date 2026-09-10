using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Services;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Authentication.Logout;

internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUserSessionStorage _userSessionStorage;
    private readonly IApplicationContext _applicationContext;

    public LogoutCommandHandler(
        IUserSessionStorage userSessionStorage,
        IApplicationContext applicationContext)
    {
        _userSessionStorage = userSessionStorage;
        _applicationContext = applicationContext;
    }

    public Task Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        _userSessionStorage.Clear();
        _applicationContext.ClearUser();

        return Task.CompletedTask;
    }
}