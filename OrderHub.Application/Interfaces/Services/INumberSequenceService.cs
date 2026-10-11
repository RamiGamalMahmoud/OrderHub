using OrderHub.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;
public interface INumberSequenceService
{
    Task<long> NextGlobalAsync(
        NumberSequenceKind kind,
        CancellationToken cancellationToken = default);

    Task<long> NextMonthlyAsync(
        NumberSequenceKind kind,
        int partyId,
        CancellationToken cancellationToken = default);
}