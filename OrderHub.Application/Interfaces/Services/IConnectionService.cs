using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IConnectionService
{
    event EventHandler<bool> ConnectionChanged;
    bool IsConnected { get; }

    Task CheckConnectionAsync(CancellationToken cancellationToken = default);
}
