using System;
using OrderHub.Domain.Models;
using Xunit;

namespace OrderHub.Tests;

public sealed class WarehouseDomainTests
{
    private static readonly City Riyadh = new("الرياض");

    [Fact]
    public void Constructor_TrimsNameAndAddressAndStartsActive()
    {
        var warehouse = new Warehouse(
            "  المخزن الرئيسي  ",
            7,
            Riyadh,
            "  شارع الملك فهد  ");

        Assert.Equal("المخزن الرئيسي", warehouse.Name.Value);
        Assert.Equal(7, warehouse.KeeperUserId);
        Assert.Equal("الرياض", warehouse.Address.City.Name.Value);
        Assert.Equal("شارع الملك فهد", warehouse.Address.Street);
        Assert.Null(warehouse.Phone);
        Assert.True(warehouse.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("م")]
    [InlineData("مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن")]
    public void Constructor_RejectsInvalidName(string name)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new Warehouse(name, 7, Riyadh, "شارع الملك فهد"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void Constructor_RejectsInvalidKeeper(int keeperUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Warehouse("مخزن", keeperUserId, Riyadh, "شارع الملك فهد"));
    }

    [Fact]
    public void Constructor_RequiresCityAndStreet()
    {
        Assert.Throws<ArgumentException>(
            () => new Warehouse("مخزن", 7, null, "شارع الملك فهد"));

        Assert.Throws<ArgumentException>(
            () => new Warehouse("مخزن", 7, Riyadh, "   "));
    }

    [Fact]
    public void Constructor_RejectsInvalidPhone()
    {
        Assert.Throws<ArgumentException>(
            () => new Warehouse("مخزن", 7, Riyadh, "شارع الملك فهد", "12", "+966"));
    }

    [Fact]
    public void UpdateAddress_ReplacesCityAndStreetInPlace()
    {
        var warehouse = new Warehouse("مخزن", 7, Riyadh, "شارع الملك فهد");
        var jeddah = new City("جدة");

        warehouse.UpdateAddress(jeddah, "  شارع التحلية  ");

        Assert.Equal("جدة", warehouse.Address.City.Name.Value);
        Assert.Equal("شارع التحلية", warehouse.Address.Street);
    }

    [Fact]
    public void UpdatePhone_AddsChangesAndClearsTheContact()
    {
        var warehouse = new Warehouse("مخزن", 7, Riyadh, "شارع الملك فهد");

        warehouse.UpdatePhone("+966", "512345678");
        Assert.Equal("+966", warehouse.Phone.Number.CountryCode);
        Assert.Equal("512345678", warehouse.Phone.Number.NationalNumber);

        Phone original = warehouse.Phone;
        warehouse.UpdatePhone("+20", "1001234567");
        Assert.Same(original, warehouse.Phone);
        Assert.Equal("+20", warehouse.Phone.Number.CountryCode);

        warehouse.UpdatePhone("+966", " ");
        Assert.Null(warehouse.Phone);
    }

    [Fact]
    public void Rename_ReplacesTheNormalizedName()
    {
        var warehouse = new Warehouse("مخزن أول", 7, Riyadh, "شارع الملك فهد");

        warehouse.Rename("  مخزن ثانٍ  ");

        Assert.Equal("مخزن ثانٍ", warehouse.Name.Value);
    }

    [Fact]
    public void AssignKeeper_ChangesTheKeeperAndRejectsInvalidIds()
    {
        var warehouse = new Warehouse("مخزن", 7, Riyadh, "شارع الملك فهد");

        warehouse.AssignKeeper(9);

        Assert.Equal(9, warehouse.KeeperUserId);
        Assert.Throws<ArgumentOutOfRangeException>(() => warehouse.AssignKeeper(0));
    }

    [Fact]
    public void DeactivateAndActivate_ToggleTheActivityFlag()
    {
        var warehouse = new Warehouse("مخزن", 7, Riyadh, "شارع الملك فهد");

        warehouse.Deactivate();
        Assert.False(warehouse.IsActive);

        warehouse.Deactivate();
        Assert.False(warehouse.IsActive);

        warehouse.Activate();
        Assert.True(warehouse.IsActive);
    }
}
