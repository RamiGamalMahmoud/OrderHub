using System;
using System.Linq;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Tests;

public sealed class TripDomainPrimitivesTests
{
    private static readonly DateTime AtUtc = new(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void External_party_normalizes_values_and_requires_all_fields()
    {
        var party = TripParty.External("  جهة خارجية  ", " 0501234567 ", "  حي النخيل ", "  https://maps.example/1 ");

        Assert.Equal(TripPartyType.External, party.PartyType);
        Assert.Null(party.PartyId);
        Assert.Equal("جهة خارجية", party.Name);
        Assert.Equal("0501234567", party.Phone);
        Assert.Equal("حي النخيل", party.Address);
        Assert.Equal("https://maps.example/1", party.Location);
    }

    [Theory]
    [InlineData(null, "0501234567", "حي النخيل")]
    [InlineData("جهة", null, "حي النخيل")]
    [InlineData("جهة", "0501234567", null)]
    public void External_party_rejects_missing_required_fields(string? name, string? phone, string? address) =>
        Assert.Throws<InvalidOperationException>(() => TripParty.External(name, phone, address, null));

    [Theory]
    [InlineData("12345")]
    [InlineData("12345678901234567890123")]
    [InlineData("abcdefgh")]
    public void External_party_rejects_invalid_phone_shapes(string phone)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => TripParty.External("جهة", phone, "حي النخيل", null));

        Assert.Contains("الهاتف", exception.Message);
    }

    [Fact]
    public void External_party_rejects_overlong_fields()
    {
        Assert.Throws<InvalidOperationException>(
            () => TripParty.External(new string('ا', 101), "0501234567", "حي النخيل", null));
        Assert.Throws<InvalidOperationException>(
            () => TripParty.External("جهة", "0501234567", new string('ا', 256), null));
        Assert.Throws<InvalidOperationException>(
            () => TripParty.External("جهة", "0501234567", "حي النخيل", new string('ا', 256)));
    }

    [Fact]
    public void Supplier_snapshot_maps_missing_optional_values_to_null()
    {
        var supplier = new Supplier("مورد بلا عنوان");

        var supplierParty = TripParty.FromSupplier(supplier);

        Assert.Equal(TripPartyType.Supplier, supplierParty.PartyType);
        Assert.Equal("مورد بلا عنوان", supplierParty.Name);
        Assert.Null(supplierParty.Phone);
        Assert.Null(supplierParty.Address);
        Assert.Null(supplierParty.Location);
    }

    [Fact]
    public void Client_snapshot_uses_city_and_street_once()
    {
        var city = new City("الرياض");
        var client = new Client("أحمد", "شارع الملك", city, "0501111111", "+966");

        var party = TripParty.FromClient(client);

        Assert.Equal(TripPartyType.Client, party.PartyType);
        Assert.Equal("أحمد", party.Name);
        Assert.Equal("+966 - 0501111111", party.Phone);
        Assert.Equal("الرياض - شارع الملك", party.Address);
        Assert.Equal(1, party.Address!.Split("الرياض").Length - 1);
    }

    [Fact]
    public void Attempt_requires_start_before_completion_or_failure()
    {
        var attempt = StandaloneTripAttempt.Pending(1, AtUtc);

        Assert.Throws<InvalidOperationException>(() => attempt.Complete(AtUtc));
        Assert.Throws<InvalidOperationException>(() => attempt.Fail("سبب", AtUtc));

        attempt.Start(AtUtc);
        Assert.Throws<InvalidOperationException>(() => attempt.Start(AtUtc.AddMinutes(1)));
        Assert.Throws<ArgumentException>(() => attempt.Fail("   ", AtUtc));

        attempt.Fail("العميل غير متوفر", AtUtc.AddMinutes(5));
        Assert.Equal(WorkRecordStatus.Failed, attempt.Status);
        Assert.Equal(AtUtc.AddMinutes(5), attempt.EndedAtUtc);
        Assert.Equal("العميل غير متوفر", attempt.Reason);
    }

    [Fact]
    public void History_factories_validate_required_reason_and_operation()
    {
        var operation = new TripOperation(1, "مستخدم", AtUtc);

        Assert.Throws<ArgumentException>(() =>
            StandaloneTripHistoryEntry.Failed(1, operation, 1, " ", WorkRecordStatus.Pending, WorkRecordStatus.Failed));

        Assert.Throws<ArgumentException>(() =>
            StandaloneTripHistoryEntry.Cancelled(1, operation, null, new string('ا', 201), WorkRecordStatus.Pending, WorkRecordStatus.Cancelled));

        Assert.Throws<ArgumentException>(() =>
            StandaloneTripHistoryEntry.Reassigned(1, operation, "سبب", 0, 2, "أول", "ثانٍ", 1, 2));

        Assert.Throws<ArgumentException>(() =>
            StandaloneTripHistoryEntry.Created(
                1,
                new TripOperation(1, "مستخدم", DateTime.SpecifyKind(AtUtc, DateTimeKind.Local)),
                null,
                WorkRecordStatus.Pending));

        var edited = StandaloneTripHistoryEntry.Edited(1, operation, "  ملاحظة  ");
        Assert.Equal("ملاحظة", edited.Reason);
        Assert.Equal(StandaloneTripEventType.Edited, edited.EventType);
    }

    [Fact]
    public void Operation_factory_rejects_anonymous_and_nameless_users()
    {
        Assert.False(TripOperationFactory.Create(TestCurrentUser.Anonymous, TripTestDb.Clock()).IsSuccess);
        Assert.False(TripOperationFactory.Create(new TestCurrentUser(5, null, null), TripTestDb.Clock()).IsSuccess);

        var result = TripOperationFactory.Create(new TestCurrentUser(5, "user", "اسم"), TripTestDb.Clock());

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.UserId);
        Assert.Equal("اسم", result.Value.UserName);
        Assert.Equal(DateTimeKind.Utc, result.Value.AtUtc.Kind);
        Assert.Equal(TripTestDb.AtUtc, result.Value.AtUtc);
    }

    [Fact]
    public void Input_validation_enforces_titles_party_ids_and_phone_rules()
    {
        var external = new TripPartyInput(TripPartyType.External, null, "جهة", "0501234567", "حي النخيل", null);
        Assert.True(TripInputValidation.ValidateCreate("عنوان", null, external, external).IsSuccess);

        Assert.False(TripInputValidation.ValidateCreate("  ", null, external, external).IsSuccess);
        Assert.False(TripInputValidation.ValidateCreate(new string('ا', 151), null, external, external).IsSuccess);
        Assert.False(TripInputValidation.ValidateCreate("عنوان", new string('ا', 1001), external, external).IsSuccess);

        var missingClient = new TripPartyInput(TripPartyType.Client, 0, null, null, null, null);
        Assert.False(TripInputValidation.ValidateCreate("عنوان", null, missingClient, external).IsSuccess);

        var shortPhone = new TripPartyInput(TripPartyType.External, null, "جهة", "123", "حي النخيل", null);
        Assert.False(TripInputValidation.ValidateCreate("عنوان", null, shortPhone, external).IsSuccess);

        var reassignmentWithoutReason = TripInputValidation.ValidateUpdate(
            "عنوان", null, external, external, 1, 2, null, "  ");
        Assert.False(reassignmentWithoutReason.IsSuccess);
        Assert.Contains("إعادة الإسناد", reassignmentWithoutReason.ErrorMessage);

        Assert.True(TripInputValidation.ValidateUpdate(
            "عنوان", null, external, external, 1, 1, new string('ا', 200), null).IsSuccess);
        Assert.False(TripInputValidation.ValidateUpdate(
            "عنوان", null, external, external, 1, 1, new string('ا', 201), null).IsSuccess);
    }

    [Fact]
    public void Enum_catalogs_match_the_specification()
    {
        Assert.Equal(6, Enum.GetValues<TripDisplayState>().Length);
        Assert.Equal(8, Enum.GetValues<StandaloneTripEventType>().Length);
        Assert.Equal(
            new[] { TripPartyType.Client, TripPartyType.Supplier, TripPartyType.ShippingCarrier, TripPartyType.External },
            Enum.GetValues<TripPartyType>().ToArray());
        Assert.Equal(
            new[]
            {
                StandaloneTripEventType.Created,
                StandaloneTripEventType.AttemptStarted,
                StandaloneTripEventType.Completed,
                StandaloneTripEventType.Failed,
                StandaloneTripEventType.Cancelled,
                StandaloneTripEventType.Retried,
                StandaloneTripEventType.Reassigned,
                StandaloneTripEventType.Edited
            },
            Enum.GetValues<StandaloneTripEventType>().ToArray());
    }

    [Fact]
    public void Lookup_kind_covers_every_supported_party()
    {
        Assert.Equal(
            new[]
            {
                TripLookupKind.Deliveryman,
                TripLookupKind.Client,
                TripLookupKind.Supplier,
                TripLookupKind.ShippingCarrier
            },
            Enum.GetValues<TripLookupKind>().ToArray());
        Assert.Equal(4, Enum.GetValues<TripLookupKind>().Distinct().Count());
    }
}
