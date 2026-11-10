using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Context;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure;

namespace OrderHub.Tests;

/// <summary>
/// In-memory SQLite database seeded with the master data every trips test needs:
/// one city, two deliverymen, two clients, a supplier, a shipping carrier, and a user.
/// </summary>
internal sealed class TripTestDb : IDisposable
{
    public static readonly DateTime AtUtc = new(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc);
    public const int SequenceYear = 2026;
    public const int SequenceMonth = 11;

    private readonly SqliteConnection _connection;

    public TripTestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        Context = new AppDbContext(Options);
        Context.Database.EnsureCreated();
        Factory = new AppDbContextFactory(Options);

        Seed();
    }

    public DbContextOptions<AppDbContext> Options { get; }

    public AppDbContext Context { get; }

    public AppDbContextFactory Factory { get; }

    public City City { get; private set; } = null!;

    public Deliveryman Deliveryman { get; private set; } = null!;

    public Deliveryman OtherDeliveryman { get; private set; } = null!;

    public Client Client { get; private set; } = null!;

    public Client SecondClient { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    public ShippingCarrier Carrier { get; private set; } = null!;

    public User User { get; private set; } = null!;

    public TripOperation Operation(DateTime? atUtc = null) =>
        new(User.Id, "مستخدم الاختبار", DateTime.SpecifyKind(atUtc ?? AtUtc, DateTimeKind.Utc));

    public static TripParty ExternalParty(string name, string phone = "0505555555", string address = "حي النخيل") =>
        TripParty.External(name, phone, address, null);

    public static FixedClock Clock(DateTime? atUtc = null) => new(atUtc ?? AtUtc);

    public StandaloneTrip PersistTrip(
        long number,
        string title = "مشوار تجريبي",
        Deliveryman? deliveryman = null,
        TripParty? from = null,
        TripParty? to = null,
        DateTimeOffset? scheduledAtUtc = null,
        DateTime? createdAtUtc = null)
    {
        var operation = Operation();
        var trip = StandaloneTrip.Create(
            (deliveryman ?? Deliveryman).Id,
            title,
            null,
            from ?? ExternalParty("المرسل"),
            to ?? ExternalParty("المستلم"),
            scheduledAtUtc,
            operation);

        trip.AssignNumber(number, SequenceYear, SequenceMonth);
        trip.UpdateCreationDate(createdAtUtc ?? AtUtc);
        Context.StandaloneTrips.Add(trip);
        Context.SaveChanges();
        return trip;
    }

    /// <summary>Loads a fresh copy with the deliveryman navigation (needed before reassignment).</summary>
    public StandaloneTrip ReloadWithDeliveryman(int tripId)
    {
        Context.ChangeTracker.Clear();
        return Context.StandaloneTrips
            .Include(trip => trip.Attempts)
            .Include(trip => trip.History)
            .Include(trip => trip.Deliveryman)
            .Single(trip => trip.Id == tripId);
    }

    public StandaloneTrip Reload(int tripId)
    {
        Context.ChangeTracker.Clear();
        return Context.StandaloneTrips
            .Include(trip => trip.Attempts)
            .Include(trip => trip.History)
            .Single(trip => trip.Id == tripId);
    }

    public DeliveryTrip AddDirectOrderTrip(
        Deliveryman deliveryman,
        long number,
        string orderNumber = "ORD-TRIP-1")
    {
        var order = new Order(Client.Id, orderNumber);
        order.UpdateHeader(Client.Id, OrderSource.Online, DeliveryMethod.DeliveryMan, deliveryman.Id, null, null);
        Context.Orders.Add(order);
        Context.SaveChanges();

        var operation = new OrderOperation(User.Id, "مستخدم الاختبار", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, operation).IsSuccess);

        var trip = order.DeliveryTrips.Single();
        ((IWorkRecord)trip).AssignWorkRecordNumber(deliveryman.Id, number, SequenceYear, SequenceMonth);
        Context.SaveChanges();
        return trip;
    }

    public DeliveryTrip AddStepOrderTrip(
        Deliveryman deliveryman,
        long number,
        string orderNumber = "ORD-TRIP-STEP")
    {
        var order = new Order(Client.Id, orderNumber);
        order.UpdateHeader(Client.Id, OrderSource.Online, DeliveryMethod.DeliveryChain, null, null, null);
        var step = new OrderDeliveryStep
        {
            StepOrder = 1,
            DeliveryMethod = DeliveryMethod.DeliveryMan,
            DeliverymanId = deliveryman.Id
        };
        order.AddDeliveryStep(step);
        Context.Orders.Add(order);
        Context.SaveChanges();

        var operation = new OrderOperation(User.Id, "مستخدم الاختبار", AtUtc);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, operation).IsSuccess);
        Assert.True(order.TransitionStep(step.Id, DeliveryStatus.InProgress, operation).IsSuccess);

        var trip = step.DeliveryTrips.Single();
        ((IWorkRecord)trip).AssignWorkRecordNumber(deliveryman.Id, number, SequenceYear, SequenceMonth);
        Context.SaveChanges();
        return trip;
    }

    private void Seed()
    {
        City = new City("الرياض");
        Context.Cities.Add(City);

        Deliveryman = new Deliveryman("سالم", City, "0500000001");
        OtherDeliveryman = new Deliveryman("ناصر", City, "0500000002");
        Context.Deliverymen.AddRange(Deliveryman, OtherDeliveryman);

        Client = new Client("أحمد", "شارع الملك", City, "0501111111", "+966");
        SecondClient = new Client("سارة", "شارع العروبة", City, "0502222222", "+966");
        Context.Clients.AddRange(Client, SecondClient);

        Supplier = new Supplier(
            "مورد الجملة",
            null,
            null,
            Address.Create("مستودع الشمال", City).Value,
            Phone.Create("0503333333", "+966").Value);
        Context.Suppliers.Add(Supplier);

        Carrier = new ShippingCarrier(
            "شركة النقل السريع",
            25m,
            Phone.Create("0504444444", "+966").Value,
            Address.Create("طريق الملك فهد", City).Value);
        Context.ShippingCarriers.Add(Carrier);

        User = User.Create("tester", "hash", "مستخدم الاختبار");
        Context.Users.Add(User);

        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

internal sealed class FixedClock(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
}

internal sealed class TestCurrentUser(int? id, string? userName = "tester", string? displayName = "Tester") : ICurrentUser
{
    public int? Id { get; } = id;

    public string? UserName { get; } = userName;

    public string? DisplayName { get; } = displayName;

    public bool IsAuthenticated => Id is > 0;

    public bool IsAdmin => false;

    public static TestCurrentUser Anonymous => new(null);
}
