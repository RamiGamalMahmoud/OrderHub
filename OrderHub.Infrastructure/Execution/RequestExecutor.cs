using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Execution;

public sealed class RequestExecutor(IServiceScopeFactory scopeFactory) : IRequestExecutor
{
    public async Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(request, cancellationToken);
    }

    public async Task ExecuteAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(request, cancellationToken);
    }
}