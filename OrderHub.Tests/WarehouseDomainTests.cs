using System;
using OrderHub.Domain.Models;
using Xunit;

namespace OrderHub.Tests;

public sealed class WarehouseDomainTests
{
    [Fact]
    public void Constructor_TrimsNameAndStartsActive()
    {
        var warehouse = new Warehouse("  المخزن الرئيسي  ", 7);

        Assert.Equal("المخزن الرئيسي", warehouse.Name.Value);
        Assert.Equal(7, warehouse.KeeperUserId);
        Assert.True(warehouse.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("م")]
    [InlineData("مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن مخزن")]
    public void Constructor_RejectsInvalidName(string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Warehouse(name, 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void Constructor_RejectsInvalidKeeper(int keeperUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Warehouse("مخزن", keeperUserId));
    }

    [Fact]
    public void Rename_ReplacesTheNormalizedName()
    {
        var warehouse = new Warehouse("مخزن أول", 7);

        warehouse.Rename("  مخزن ثانٍ  ");

        Assert.Equal("مخزن ثانٍ", warehouse.Name.Value);
    }

    [Fact]
    public void AssignKeeper_ChangesTheKeeperAndRejectsInvalidIds()
    {
        var warehouse = new Warehouse("مخزن", 7);

        warehouse.AssignKeeper(9);

        Assert.Equal(9, warehouse.KeeperUserId);
        Assert.Throws<ArgumentOutOfRangeException>(() => warehouse.AssignKeeper(0));
    }

    [Fact]
    public void DeactivateAndActivate_ToggleTheActivityFlag()
    {
        var warehouse = new Warehouse("مخزن", 7);

        warehouse.Deactivate();
        Assert.False(warehouse.IsActive);

        warehouse.Deactivate();
        Assert.False(warehouse.IsActive);

        warehouse.Activate();
        Assert.True(warehouse.IsActive);
    }
}
