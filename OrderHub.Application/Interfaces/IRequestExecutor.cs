using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces;

public interface IRequestExecutor
{
    Task<TResponse> ExecuteAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(IRequest request, CancellationToken cancellationToken = default);
}