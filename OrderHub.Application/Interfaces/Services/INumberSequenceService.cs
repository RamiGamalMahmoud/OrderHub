using OrderHub.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;
public interface INumberSequenceService
{
    Task<long> NextGlobalAsync(
        NumberSequenceKind kind,
        CancellationToken cancellationToken = default);

    Task<MonthlyNumberAllocation> NextMonthlyAsync(
        NumberSequenceKind kind,
        int partyId,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken = default);
}
