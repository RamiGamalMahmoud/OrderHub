using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Common
{
    public interface IUseCase<in TRequest, TResponse>
    {
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken ct = default);
    }

    public interface IUseCase<TResponse>
    {
        Task<TResponse> ExecuteAsync(CancellationToken ct = default);
    }
}
