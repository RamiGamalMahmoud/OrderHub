using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;

namespace OrderHub.Web.Services;

public sealed class AuthenticatedRequestExecutor(IServiceScopeFactory scopeFactory, ICurrentUser currentUser)
    : IRequestExecutor
{
    public async Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<OperationUser>().Initialize(userId);
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request, cancellationToken);
    }

    public async Task ExecuteAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<OperationUser>().Initialize(userId);
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request, cancellationToken);
    }
}
