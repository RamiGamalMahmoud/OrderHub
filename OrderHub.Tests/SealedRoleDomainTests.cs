using OrderHub.Domain.Authorization;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
using System.Linq;

namespace OrderHub.Tests;

public sealed class SealedRoleDomainTests
{
    [Fact]
    public void Role_Sealed_Rename_Throws()
    {
        Role role = Role.CreateSealed(
            SealedRolePresets.SalesKey,
            "مبيعات");

        Assert.Throws<InvalidOperationException>(
            () => role.Rename("اسم آخر"));

        Assert.Equal("مبيعات", role.Name);
    }

    [Fact]
    public void Role_Sealed_SetPermission_Throws()
    {
        Role role = Role.CreateSealed(
            SealedRolePresets.SalesKey,
            "مبيعات");

        Assert.Throws<InvalidOperationException>(
            () => role.SetPermission(
                PermissionFeature.Orders,
                PermissionLevel.Read));

        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void Role_CreateSealed_SetsKeyAndFlag()
    {
        Role role = Role.CreateSealed(
            SealedRolePresets.SalesKey,
            "  مبيعات  ");

        Assert.True(role.IsSealed);
        Assert.Equal("sales", role.RoleKey);
        Assert.Equal("مبيعات", role.Name);
    }

    [Fact]
    public void Role_SetPermission_Approve_Persists()
    {
        var role = new Role("مدير فرع");

        role.SetPermission(
            PermissionFeature.Orders,
            PermissionLevel.Approve);

        RolePermission permission = Assert.Single(role.Permissions);
        Assert.Equal(PermissionLevel.Approve, permission.Level);
    }

    [Fact]
    public void RolePermission_None_StillThrows()
    {
        var role = new Role("دور");

        Assert.Throws<ArgumentException>(
            () => new RolePermission(
                role,
                PermissionFeature.Orders,
                PermissionLevel.None));
    }

    [Fact]
    public void SealedRolePresets_Matrix_MatchesApprovedMatrix()
    {
        Assert.Equal(5, SealedRolePresets.All.Count);
        Assert.Equal(5, SealedRolePresets.All.Select(p => p.RoleKey).Distinct().Count());
        Assert.Equal(5, SealedRolePresets.All.Select(p => p.Name).Distinct().Count());

        AssertMatrix(
            SealedRolePresets.SalesKey,
            "مبيعات",
            [
                new(PermissionFeature.Orders, PermissionLevel.Write),
                new(PermissionFeature.Documents, PermissionLevel.Read),
                new(PermissionFeature.Products, PermissionLevel.Read),
                new(PermissionFeature.Categories, PermissionLevel.Read),
                new(PermissionFeature.Clients, PermissionLevel.Write),
                new(PermissionFeature.Deliverymen, PermissionLevel.Read),
                new(PermissionFeature.ShippingCarriers, PermissionLevel.Read),
                new(PermissionFeature.Cities, PermissionLevel.Read),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Read),
                new(PermissionFeature.Inventory, PermissionLevel.Read)
            ]);

        AssertMatrix(
            SealedRolePresets.PurchasingKey,
            "مشتريات",
            [
                new(PermissionFeature.Orders, PermissionLevel.Read),
                new(PermissionFeature.Documents, PermissionLevel.Read),
                new(PermissionFeature.Products, PermissionLevel.Read),
                new(PermissionFeature.Suppliers, PermissionLevel.Write),
                new(PermissionFeature.Cities, PermissionLevel.Read),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Read),
                new(PermissionFeature.Inventory, PermissionLevel.Read)
            ]);

        AssertMatrix(
            SealedRolePresets.DataEntryKey,
            "إدخال البيانات",
            [
                new(PermissionFeature.Products, PermissionLevel.Write),
                new(PermissionFeature.Categories, PermissionLevel.Write),
                new(PermissionFeature.Properties, PermissionLevel.Write),
                new(PermissionFeature.Cities, PermissionLevel.Write),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Read)
            ]);

        AssertMatrix(
            SealedRolePresets.ManagerKey,
            "مدير",
            [
                new(PermissionFeature.Orders, PermissionLevel.Approve),
                new(PermissionFeature.Documents, PermissionLevel.Approve),
                new(PermissionFeature.Products, PermissionLevel.Approve),
                new(PermissionFeature.Categories, PermissionLevel.Approve),
                new(PermissionFeature.Properties, PermissionLevel.Approve),
                new(PermissionFeature.Clients, PermissionLevel.Approve),
                new(PermissionFeature.Suppliers, PermissionLevel.Approve),
                new(PermissionFeature.Deliverymen, PermissionLevel.Approve),
                new(PermissionFeature.ShippingCarriers, PermissionLevel.Approve),
                new(PermissionFeature.Cities, PermissionLevel.Approve),
                new(PermissionFeature.PaymentMethods, PermissionLevel.Approve),
                new(PermissionFeature.Warehouses, PermissionLevel.Write),
                new(PermissionFeature.Inventory, PermissionLevel.Approve)
            ]);

        AssertMatrix(
            SealedRolePresets.StorekeeperKey,
            "أمين مخزن",
            [
                new(PermissionFeature.Warehouses, PermissionLevel.Read),
                new(PermissionFeature.Products, PermissionLevel.Read),
                new(PermissionFeature.Inventory, PermissionLevel.Write)
            ]);

        Assert.DoesNotContain(
            SealedRolePresets.All.SelectMany(p => p.Permissions),
            p => p.Feature is PermissionFeature.Logs
                or PermissionFeature.Users
                or PermissionFeature.Settings);
    }

    [Fact]
    public void SealedRolePresets_ReservedNames_AreTrimmedAndCaseInsensitive()
    {
        Assert.True(SealedRolePresets.IsReservedName("  مبيعات "));
        Assert.True(SealedRolePresets.IsReservedName("إدخال البيانات"));
        Assert.True(SealedRolePresets.IsReservedName(" مدير "));
        Assert.True(SealedRolePresets.IsReservedName("أمين مخزن"));
        Assert.False(SealedRolePresets.IsReservedName("مبيعات فرع"));
        Assert.False(SealedRolePresets.IsReservedName("أمين مخزن فرع"));
        Assert.False(SealedRolePresets.IsReservedName("مدير فرع"));
        Assert.False(SealedRolePresets.IsReservedName(" "));
        Assert.False(SealedRolePresets.IsReservedName(null));
    }

    [Fact]
    public void SealedRolePresets_FindByKey_MatchesOnlyExactKey()
    {
        SealedRolePreset? manager = SealedRolePresets.FindByKey(
            SealedRolePresets.ManagerKey);

        Assert.NotNull(manager);
        Assert.Equal("مدير", manager.Name);
        Assert.Null(SealedRolePresets.FindByKey("Manager"));
        Assert.Null(SealedRolePresets.FindByKey("unknown"));
        Assert.Null(SealedRolePresets.FindByKey(null));
    }

    private static void AssertMatrix(
        string roleKey,
        string expectedName,
        RolePermissionPreset[] expected)
    {
        SealedRolePreset preset = Assert.Single(
            SealedRolePresets.All,
            candidate => candidate.RoleKey == roleKey);

        Assert.Equal(expectedName, preset.Name);

        Assert.Equal(
            expected
                .OrderBy(cell => cell.Feature)
                .ThenBy(cell => cell.Level)
                .ToList(),
            preset.Permissions
                .OrderBy(cell => cell.Feature)
                .ThenBy(cell => cell.Level)
                .ToList());

        Assert.DoesNotContain(
            preset.Permissions,
            cell => cell.Level == PermissionLevel.None);
    }
}
