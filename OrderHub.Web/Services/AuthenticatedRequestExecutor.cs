using MediatR;
using OrderHub.Application.Interfaces;

namespace OrderHub.Web.Services;

public sealed class AuthenticatedRequestExecutor(IServiceScopeFactory scopeFactory, AuthenticatedUser authenticatedUser)
    : IRequestExecutor
{
    public async Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var identity = await authenticatedUser.GetIdentityAsync(cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<OperationUser>().Initialize(identity);
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request, cancellationToken);
    }

    public async Task ExecuteAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var identity = await authenticatedUser.GetIdentityAsync(cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<OperationUser>().Initialize(identity);
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request, cancellationToken);
    }
}
