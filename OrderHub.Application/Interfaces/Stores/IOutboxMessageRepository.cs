using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Stores;

public interface IOutboxMessageRepository
{
    Task<OutboxMessage> Create(OutboxMessage mssage, CancellationToken cancellationToken = default);
    Task DeleteAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<OutboxMessage> GetForResend(int id, CancellationToken cancellationToken = default);
}
