using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Infrastructure.Connection;

public interface IConnectionConfigurationStorage
{
    Task SaveAsync(ConnectionConfiguration configuration, CancellationToken cancellationToken = default);

    Task<ConnectionConfiguration> LoadAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(CancellationToken cancellationToken = default);
}