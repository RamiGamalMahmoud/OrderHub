using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Trips.CancelTrip;
using OrderHub.Application.Features.Trips.CompleteTrip;
using OrderHub.Application.Features.Trips.CreateTrip;
using OrderHub.Application.Features.Trips.FailTrip;
using OrderHub.Application.Features.Trips.RetryTrip;
using OrderHub.Application.Features.Trips.StartTrip;
using OrderHub.Application.Features.Trips.UpdateTrip;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Features.Setup.Clients;
using OrderHub.Infrastructure.Features.Setup.Deliverymen;
using OrderHub.Infrastructure.Features.Setup.ShippingCarriers;
using OrderHub.Infrastructure.Features.Setup.Suppliers;
using OrderHub.Infrastructure.Features.Trips;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests;

/// <summary>
/// Monthly numbering fake that mirrors the production Riyadh period calculation
/// (the real implementation executes PostgreSQL-specific SQL, so handler tests
/// fake numbering to keep allocation deterministic).
/// </summary>
internal sealed class FakeNumberSequenceService : INumberSequenceService
{
    private static readonly TimeZoneInfo RiyadhTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");

    private readonly List<int> _partyIds = new();
    private readonly List<DateTimeOffset> _timestamps = new();

    public long NextNumber { get; set; } = 1;

    public int Calls => _timestamps.Count;

    public IReadOnlyList<int> PartyIds => _partyIds;

    public IReadOnlyList<DateTimeOffset> Timestamps => _timestamps;

    public Exception? Failure { get; set; }

    public Task<long> NextGlobalAsync(
        NumberSequenceKind kind,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(NextNumber++);

    public Task<MonthlyNumberAllocation> NextMonthlyAsync(
        NumberSequenceKind kind,
        int partyId,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        _partyIds.Add(partyId);
        _timestamps.Add(atUtc);

        var local = TimeZoneInfo.ConvertTime(atUtc, RiyadhTimeZone);
        return Task.FromResult(new MonthlyNumberAllocation(NextNumber++, local.Year, local.Month));
    }
}

/// <summary>
/// Wires the real repositories and unit of work against the seeded PostgreSQL test
/// database, faking only numbering, identity, and the clock.
/// </summary>
internal sealed class TripHandlerTestContext
{
    public TripHandlerTestContext(TripTestDb db, DateTime? atUtc = null)
    {
        Db = db;
        Sequence = new FakeNumberSequenceService();
        UnitOfWork = new UnitOfWork(db.Context, Sequence);
        Repository = new StandaloneTripRepository(db.Context);
        Deliverymen = new DeliverymanRepository(db.Context);
        Clients = new ClientRepository(db.Context);
        Suppliers = new SupplierRepository(db.Context);
        Carriers = new ShippingCarrierRepository(db.Context);
        Clock = new FixedClock(atUtc ?? TripTestDb.AtUtc);
        User = new TestCurrentUser(db.User.Id, "tester", "مستخدم الاختبار");
    }

    public TripTestDb Db { get; }

    public FakeNumberSequenceService Sequence { get; }

    public IUnitOfWork UnitOfWork { get; }

    public StandaloneTripRepository Repository { get; }

    public DeliverymanRepository Deliverymen { get; }

    public ClientRepository Clients { get; }

    public SupplierRepository Suppliers { get; }

    public ShippingCarrierRepository Carriers { get; }

    public FixedClock Clock { get; }

    public TestCurrentUser User { get; }

    public CreateTripCommandHandler CreateHandler(TestCurrentUser? user = null) =>
        new(Repository, Deliverymen, Clients, Suppliers, Carriers, Sequence, UnitOfWork, user ?? User, Clock);

    public UpdateTripCommandHandler UpdateHandler(TestCurrentUser? user = null) =>
        new(Repository, Deliverymen, Clients, Suppliers, Carriers, Sequence, UnitOfWork, user ?? User, Clock);

    public StartTripCommandHandler StartHandler(TestCurrentUser? user = null) =>
        new(Repository, UnitOfWork, user ?? User, Clock);

    public CompleteTripCommandHandler CompleteHandler(TestCurrentUser? user = null) =>
        new(Repository, UnitOfWork, user ?? User, Clock);

    public FailTripCommandHandler FailHandler(TestCurrentUser? user = null) =>
        new(Repository, UnitOfWork, user ?? User, Clock);

    public CancelTripCommandHandler CancelHandler(TestCurrentUser? user = null) =>
        new(Repository, UnitOfWork, user ?? User, Clock);

    public RetryTripCommandHandler RetryHandler(TestCurrentUser? user = null) =>
        new(Repository, UnitOfWork, user ?? User, Clock);
}
