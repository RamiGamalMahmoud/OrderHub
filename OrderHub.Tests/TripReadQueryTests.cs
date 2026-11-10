using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Application.Features.Trips.GetTripDetails;
using OrderHub.Application.Features.Trips.GetTripEditor;
using OrderHub.Application.Features.Trips.GetTripLookups;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure.Features.Setup.Clients;
using OrderHub.Infrastructure.Features.Setup.Deliverymen;
using OrderHub.Infrastructure.Features.Setup.ShippingCarriers;
using OrderHub.Infrastructure.Features.Setup.Suppliers;
using OrderHub.Infrastructure.Features.Trips;

namespace OrderHub.Tests;

public sealed class TripReadQueryTests
{
    [Fact]
    public async Task Details_project_snapshots_attempts_and_paged_history()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        trip.Update(
            "عنوان معدل",
            null,
            trip.From,
            trip.To,
            null,
            trip.DeliverymanId,
            null,
            null,
            "ملاحظة تعديل",
            db.Operation());
        db.Context.SaveChanges();
        trip.Start(db.Operation());
        db.Context.SaveChanges();

        var details = await Reader(db).GetDetailsAsync(trip.Id, 1, 10, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(trip.Id, details!.Id);
        Assert.Equal(3, details.Version);
        Assert.False(details.IsEditable);
        Assert.Equal(WorkRecordStatus.Pending, details.Status);
        Assert.Equal(TripDisplayState.InProgress, details.DisplayState);
        Assert.Equal(TripTestDb.AtUtc, details.CreatedAt);
        Assert.Equal(db.Deliveryman.Id, details.DeliverymanId);
        Assert.Equal("سالم", details.DeliverymanName);

        Assert.Equal("المرسل", details.From.Name);
        Assert.Equal("0505555555", details.From.Phone);
        Assert.Equal("حي النخيل", details.From.Address);
        Assert.Equal("المستلم", details.To.Name);

        var attempt = Assert.Single(details.Attempts);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(WorkRecordStatus.Pending, attempt.Status);
        Assert.Equal(TripTestDb.AtUtc, attempt.StartedAtUtc);
        Assert.Null(attempt.EndedAtUtc);

        Assert.Equal(1, details.CurrentAttemptNumber);
        Assert.Equal(1, details.AttemptCount);
        Assert.False(details.HasFailedAttempt);

        Assert.Equal(3, details.History.TotalCount);
        Assert.Equal(3, details.History.Items.Count());
        Assert.Equal(StandaloneTripEventType.AttemptStarted, details.History.Items.First().EventType);
        Assert.Contains(
            details.History.Items,
            entry => entry.EventType == StandaloneTripEventType.Edited && entry.Reason == "ملاحظة تعديل");
    }

    [Fact]
    public async Task Details_clamp_history_paging()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        for (int index = 0; index < 11; index++)
        {
            trip.Update(
                $"عنوان {index}",
                null,
                trip.From,
                trip.To,
                null,
                trip.DeliverymanId,
                null,
                null,
                $"ملاحظة {index}",
                db.Operation());
        }

        db.Context.SaveChanges();
        var reader = Reader(db);

        var clamped = await reader.GetDetailsAsync(trip.Id, 0, 5, CancellationToken.None);
        Assert.NotNull(clamped);
        Assert.Equal(1, clamped!.History.PageNumber);
        Assert.Equal(10, clamped.History.PageSize);
        Assert.Equal(12, clamped.History.TotalCount);
        Assert.Equal(10, clamped.History.Items.Count());

        var secondPage = await reader.GetDetailsAsync(trip.Id, 2, 10, CancellationToken.None);
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage!.History.PageNumber);
        Assert.Equal(10, secondPage.History.PageSize);
        Assert.Equal(2, secondPage.History.Items.Count());
    }

    [Fact]
    public async Task Details_mark_new_trips_editable_and_unknown_trips_not_found()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        var reader = Reader(db);

        var details = await reader.GetDetailsAsync(trip.Id, 1, 10, CancellationToken.None);
        Assert.NotNull(details);
        Assert.True(details!.IsEditable);
        Assert.Equal(TripDisplayState.New, details.DisplayState);
        Assert.Empty(details.Attempts);
        Assert.Equal(1, details.History.TotalCount);

        Assert.Null(await reader.GetDetailsAsync(4242, 1, 10, CancellationToken.None));

        var result = await new GetTripDetailsQueryHandler(reader)
            .Handle(new GetTripDetailsQuery(4242), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
    }

    [Fact]
    public async Task Details_keep_city_once_and_supplier_nulls_surface_as_null()
    {
        using var db = new TripTestDb();
        var supplier = new Supplier("مورد بلا بيانات");
        db.Context.Suppliers.Add(supplier);
        db.Context.SaveChanges();

        var trip = db.PersistTrip(1, from: TripParty.FromClient(db.Client), to: TripParty.FromSupplier(supplier));
        var details = await Reader(db).GetDetailsAsync(trip.Id, 1, 10, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal("الرياض - شارع الملك", details!.From.Address);
        Assert.Equal(1, details.From.Address!.Split("الرياض", StringSplitOptions.None).Length - 1);
        Assert.Equal(TripPartyType.Supplier, details.To.PartyType);
        Assert.Equal("مورد بلا بيانات", details.To.Name);
        Assert.Null(details.To.Phone);
        Assert.Null(details.To.Address);
        Assert.Null(details.To.Location);
    }

    [Fact]
    public async Task Editor_returns_inputs_version_and_editability()
    {
        using var db = new TripTestDb();
        var trip = db.PersistTrip(1);
        var reader = Reader(db);

        var editor = await reader.GetEditorAsync(trip.Id, CancellationToken.None);
        Assert.NotNull(editor);
        Assert.Equal(1, editor!.Version);
        Assert.True(editor.IsEditable);
        Assert.Equal(db.Deliveryman.Id, editor.DeliverymanId);
        Assert.Equal("سالم", editor.DeliverymanName);
        Assert.Equal("مشوار تجريبي", editor.Title);
        Assert.Equal(TripPartyType.External, editor.From.PartyType);
        Assert.Equal("المرسل", editor.From.Name);
        Assert.Equal("0505555555", editor.To.Phone);
        Assert.Null(editor.ScheduledAtUtc);

        trip.Start(db.Operation());
        db.Context.SaveChanges();

        var locked = await reader.GetEditorAsync(trip.Id, CancellationToken.None);
        Assert.NotNull(locked);
        Assert.False(locked!.IsEditable);
        Assert.Equal(2, locked.Version);

        Assert.Null(await reader.GetEditorAsync(4242, CancellationToken.None));

        var result = await new GetTripEditorQueryHandler(reader)
            .Handle(new GetTripEditorQuery(4242), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.ErrorCode);
    }

    [Fact]
    public async Task Lookups_search_master_data_and_cap_take()
    {
        using var db = new TripTestDb();
        for (int index = 0; index < 22; index++)
        {
            db.Context.Deliverymen.Add(new Deliveryman($"مندوب {index:D2}", db.City, $"05000001{index:D2}"));
        }

        db.Context.SaveChanges();
        var handler = LookupHandler(db);

        var deliverymen = await handler.Handle(
            new GetTripLookupsQuery(TripLookupKind.Deliveryman, "سال"),
            CancellationToken.None);
        var salem = Assert.Single(deliverymen);
        Assert.Equal("سالم", salem.Name);
        Assert.Equal("الرياض · 0500000001", salem.Detail);

        var capped = await handler.Handle(
            new GetTripLookupsQuery(TripLookupKind.Deliveryman, null, 100),
            CancellationToken.None);
        Assert.Equal(20, capped.Count);

        var atLeastOne = await handler.Handle(
            new GetTripLookupsQuery(TripLookupKind.Deliveryman, null, 0),
            CancellationToken.None);
        Assert.Single(atLeastOne);

        var clients = await handler.Handle(
            new GetTripLookupsQuery(TripLookupKind.Client, "أحمد"),
            CancellationToken.None);
        var client = Assert.Single(clients);
        Assert.Equal("الرياض - شارع الملك · +966 - 0501111111", client.Detail);

        var carriers = await handler.Handle(
            new GetTripLookupsQuery(TripLookupKind.ShippingCarrier, "النقل"),
            CancellationToken.None);
        var carrier = Assert.Single(carriers);
        Assert.Equal("الرياض - طريق الملك فهد · +966 - 0504444444", carrier.Detail);

        Assert.Empty(await handler.Handle(
            new GetTripLookupsQuery((TripLookupKind)99),
            CancellationToken.None));
    }

    [Fact]
    public async Task Supplier_lookup_without_contact_data_has_null_detail()
    {
        using var db = new TripTestDb();
        db.Context.Suppliers.Add(new Supplier("مورد بلا بيانات"));
        db.Context.SaveChanges();

        var items = await LookupHandler(db).Handle(
            new GetTripLookupsQuery(TripLookupKind.Supplier, "بلا بيانات"),
            CancellationToken.None);

        var item = Assert.Single(items);
        Assert.Equal("مورد بلا بيانات", item.Name);
        Assert.Null(item.Detail);
    }

    [Fact]
    public void Read_queries_require_the_expected_permission_levels()
    {
        AssertPermission(typeof(GetTripDetailsQuery), PermissionLevel.Read);
        AssertPermission(typeof(GetTripLookupsQuery), PermissionLevel.Read);
        AssertPermission(typeof(GetTripEditorQuery), PermissionLevel.Write);
    }

    private static void AssertPermission(Type queryType, PermissionLevel level)
    {
        var attribute = queryType.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(PermissionFeature.Trips, attribute!.Feature);
        Assert.Equal(level, attribute.RequiredLevel);
    }

    private static TripReader Reader(TripTestDb db) =>
        new(db.Factory, new StandaloneTripRepository(db.Context));

    private static GetTripLookupsQueryHandler LookupHandler(TripTestDb db) =>
        new(
            new DeliverymanReader(db.Factory),
            new ClientReader(db.Factory),
            new SupplierReader(db.Factory),
            new ShippingCarrierReader(db.Factory));
}
